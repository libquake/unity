using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Events;
using UnityEngine;

namespace LibQuake
{
    /// <summary>
    /// Imports .map files as a prefab: worldspawn with its mesh, collision and lightmap, plus one object per
    /// entity, grouped by class name.
    /// </summary>
    [ScriptedImporter(3, "map", AllowCaching = true)]
    public sealed class QMapAssetImporter : ScriptedImporter
    {
        [Tooltip("Map config for this map, the project default from Project Settings > LibQuake when empty.")]
        public MapConfigData configData;

        // state of the running import, the importer object is reused for later imports
        private MapConfigData _config;
        private NativeQFMap _nativeMap;
        private SolidEntityGenerator _solidEntityGenerator;
        private List<string> _requiredWads;
        private bool _lightmapped;
        private readonly Dictionary<string, Material> _usedMaterials = new Dictionary<string, Material>();
        private readonly Dictionary<string, int> _classCount = new Dictionary<string, int>();
        private readonly Dictionary<string, GameObject> _entityFolders = new Dictionary<string, GameObject>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // TrenchBroom's autosaves
            if (ctx.assetPath.Contains("autosave/"))
                return;

            _usedMaterials.Clear();
            _classCount.Clear();
            _entityFolders.Clear();

            _config = configData;
            if (_config == null)
            {
                var settings = ProjectSettingsData.GetSettings();
                if (settings != null)
                {
                    ctx.DependsOnSourceAsset(ProjectSettingsData.SettingsPath);
                    _config = settings.GetMapConfigData();
                }
            }

            if (_config == null)
            {
                ctx.LogImportError($"[LibQuake] Cannot import {ctx.assetPath}: no map config. Assign one or create " +
                                   "the project settings via Tools > LibQuake > Initialize Project Settings.");
                return;
            }

            // reimport when the config changes
            var configPath = AssetDatabase.GetAssetPath(_config);
            if (!string.IsNullOrEmpty(configPath))
                ctx.DependsOnSourceAsset(configPath);

            _solidEntityGenerator = new SolidEntityGenerator(_config);
            try
            {
                using (_nativeMap = new NativeQFMap())
                    Import(ctx);
            }
            finally
            {
                _nativeMap = null;
                _config = null;
            }
        }

        private void Import(AssetImportContext ctx)
        {
            _nativeMap.Load(ctx.assetPath, enableCSG: _config.enableCSG, convertToOpenGL: false,
                luxelSize: _config.luxelSize);
            _requiredWads = _nativeMap.GetRequiredWads();

            foreach (var textureName in _nativeMap.GetTextureNames())
            {
                var tex = MaterialManager.Instance.GetTexture(textureName, _nativeMap.GetTextureFlags(textureName),
                    _config, _requiredWads);
                if (tex != null)
                {
                    _nativeMap.RegisterTextureSize(textureName, (uint)tex.width, (uint)tex.height);
                }
                else
                {
                    _nativeMap.RegisterTextureSize(textureName, 64, 64);
                    ctx.LogImportWarning($"[LibQuake] Texture '{textureName}' not found, using default size 64x64");
                }
            }

            // face types only have an effect before the geometry is built. sky faces are drawn with the sky material
            _nativeMap.SetFaceTypes(_config.clipTexture, SurfaceType.CLIP);
            _nativeMap.SetFaceTypes(_config.skipTexture, SurfaceType.SKIP);

            _nativeMap.GenerateGeometry();
            _nativeMap.ExportData();

            var worldSpawnObj = new GameObject("worldSpawn") { isStatic = true };
            ctx.AddObjectToAsset("worldSpawn", worldSpawnObj);
            ctx.SetMainObject(worldSpawnObj);

            var lightmap = _config.bakeLightmap ? CreateLightmapTexture(ctx) : null;
            _lightmapped = lightmap != null;
            if (_lightmapped)
                worldSpawnObj.AddComponent<MapLightmap>().lightmap = lightmap;

            if (_nativeMap.SolidEntities.Count > 0)
                SetupWorldspawn(ctx, _nativeMap.SolidEntities[0], worldSpawnObj);

            foreach (var pent in _nativeMap.PointEntities)
            {
                var entObj = CreatePointEntityObject(ctx, NextEntityName(pent.ClassName), pent);
                if (entObj != null)
                    entObj.transform.parent = GetEntityFolder(ctx, pent.ClassName, worldSpawnObj).transform;
            }

            for (int i = 1; i < _nativeMap.SolidEntities.Count; i++)
            {
                var sent = _nativeMap.SolidEntities[i];
                var entObj = CreateSolidEntityObject(ctx, NextEntityName(sent.ClassName), sent);
                entObj.transform.parent = GetEntityFolder(ctx, sent.ClassName, worldSpawnObj).transform;
            }

            foreach (var mat in _usedMaterials.Values)
                ctx.AddObjectToAsset(mat.name, mat);

            SetupEvents(worldSpawnObj);
        }

        private void SetupWorldspawn(AssetImportContext ctx, MapSolidEntity worldspawn, GameObject obj)
        {
            var renderMesh = _solidEntityGenerator.GenerateRenderMesh(worldspawn, "worldspawn_mesh", GetMaterial,
                out var materials);
            if (renderMesh != null)
            {
                obj.AddComponent<MeshFilter>().sharedMesh = renderMesh;
                obj.AddComponent<MeshRenderer>().sharedMaterials = materials;
                ctx.AddObjectToAsset("worldspawn_mesh", renderMesh);
            }

            // static world collision from the brush hulls, clip brushes block too, liquids are volumes
            var collisionMesh = _solidEntityGenerator.GenerateCollisionMesh(worldspawn, "worldspawn_collision",
                t => t != HullType.Liquid);
            if (collisionMesh != null)
            {
                obj.AddComponent<MeshCollider>().sharedMesh = collisionMesh;
                ctx.AddObjectToAsset("worldspawn_collision", collisionMesh);
            }

            AddLiquidVolumes(ctx, worldspawn, obj);
        }

        /// <summary>
        /// "classname_N", numbered per class in import order.
        /// </summary>
        private string NextEntityName(string className)
        {
            _classCount.TryGetValue(className, out var count);
            _classCount[className] = count + 1;
            return $"{className}_{count}";
        }

        /// <summary>
        /// One material per texture and import, shared by all entities.
        /// </summary>
        private Material GetMaterial(string textureName)
        {
            if (!_usedMaterials.TryGetValue(textureName, out var mat))
            {
                mat = MaterialManager.Instance.CreateMaterial(textureName, _nativeMap.GetTextureFlags(textureName),
                    _config, _requiredWads, _lightmapped);
                _usedMaterials.Add(textureName, mat);
            }

            return mat;
        }

        /// <summary>
        /// Bakes the lightmap with libquake (map lights) and stores it as a sub asset. The rows are uploaded as
        /// they come, so the lightmap uvs need no flip. Values follow Quake: 128 is normal brightness.
        /// </summary>
        private Texture2D CreateLightmapTexture(AssetImportContext ctx)
        {
            var lm = _nativeMap.GetLightmap();
            if (lm == null)
                return null;

            var tex = new Texture2D(lm.Width, lm.Height, TextureFormat.RGBA32, false, true)
            {
                name = "lightmap",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixelData(lm.Combined, 0);
            tex.Apply(false, true);
            ctx.AddObjectToAsset("lightmap", tex);
            return tex;
        }

        private void AddLiquidVolumes(AssetImportContext ctx, MapSolidEntity entity, GameObject parent)
        {
            var liquids = _solidEntityGenerator.GenerateHullMeshes(entity, "liquid", t => t == HullType.Liquid);
            if (liquids.Count == 0)
                return;
            var obj = new GameObject("liquids");
            obj.transform.parent = parent.transform;
            ctx.AddObjectToAsset("liquids", obj);
            foreach (var mesh in liquids)
            {
                var col = obj.AddComponent<MeshCollider>();
                col.convex = true;
                col.isTrigger = true;
                col.sharedMesh = mesh;
                ctx.AddObjectToAsset(mesh.name, mesh);
            }
        }

        private GameObject GetEntityFolder(AssetImportContext ctx, string className, GameObject worldSpawnObj)
        {
            if (!_entityFolders.TryGetValue(className, out var folderObj))
            {
                folderObj = new GameObject(className);
                folderObj.transform.parent = worldSpawnObj.transform;
                _entityFolders[className] = folderObj;
                ctx.AddObjectToAsset(className + "_folder", folderObj);
            }

            return folderObj;
        }

        /// <summary>
        /// Null if the map config has no prefab for the class.
        /// </summary>
        private GameObject CreatePointEntityObject(AssetImportContext ctx, string name, MapPointEntity ent)
        {
            foreach (var entry in _config.pointEntities)
            {
                if (ent.ClassName != entry.className || entry.prefab == null) continue;
                var obj = Instantiate(entry.prefab);
                entry.SetupPrefab(obj, ent.Attributes, ent.Origin, ent.Angle, _config.inverseScale);
                obj.name = name;
                ctx.AddObjectToAsset(name, obj);
                return obj;
            }

            return null;
        }

        private GameObject CreateSolidEntityObject(AssetImportContext ctx, string name, MapSolidEntity ent)
        {
            var obj = _solidEntityGenerator.GetGameObjectForSolidEntity(ent);
            obj.name = name;
            ctx.AddObjectToAsset(name, obj);

            var mesh = _solidEntityGenerator.GenerateRenderMesh(ent, name + "_mesh", GetMaterial, out var materials);
            if (mesh != null)
            {
                var mf = obj.GetComponent<MeshFilter>();
                if (mf == null)
                    mf = obj.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = obj.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterials = materials;
                ctx.AddObjectToAsset(name + "_mesh", mesh);
            }

            var mc = obj.GetComponent<MeshCollider>();
            if (mc != null)
                SetupCollider(ctx, obj, mc, ent, name);

            return obj;
        }

        /// <summary>
        /// Brush hulls as colliders. Triggers and convex colliders get one convex collider per brush, Unity cannot
        /// use a non convex mesh for triggers. Everything else gets one mesh with all brushes.
        /// </summary>
        private void SetupCollider(AssetImportContext ctx, GameObject obj, MeshCollider mc, MapSolidEntity ent, string name)
        {
            if (mc.isTrigger || mc.convex)
            {
                var hulls = _solidEntityGenerator.GenerateHullMeshes(ent, name + "_hull", _ => true);
                if (hulls.Count == 0)
                {
                    mc.enabled = false;
                    return;
                }

                for (int i = 0; i < hulls.Count; i++)
                {
                    var col = i == 0 ? mc : obj.AddComponent<MeshCollider>();
                    col.convex = true;
                    col.isTrigger = mc.isTrigger;
                    col.sharedMaterial = mc.sharedMaterial;
                    col.sharedMesh = hulls[i];
                    ctx.AddObjectToAsset(hulls[i].name, hulls[i]);
                }

                return;
            }

            var collision = _solidEntityGenerator.GenerateCollisionMesh(ent, name + "_collision", t => t != HullType.Liquid);
            if (collision == null)
            {
                mc.enabled = false;
                return;
            }

            mc.sharedMesh = collision;
            ctx.AddObjectToAsset(collision.name, collision);
        }

        /// <summary>
        /// Connects target/targetname: receivers listen on the map's event bus, emitters fire into it.
        /// </summary>
        private static void SetupEvents(GameObject worldSpawnObj)
        {
            var evbus = worldSpawnObj.AddComponent<EntityEventBus>();

            foreach (var c in worldSpawnObj.GetComponentsInChildren<EntityEventReceiver>())
                UnityEventTools.AddVoidPersistentListener(evbus.GetOrAddEvent(c.targetName), c.OnTrigger);

            foreach (var c in worldSpawnObj.GetComponentsInChildren<EntityEventEmitter>())
                c.SetLocalEventBus(evbus);
        }
    }
}
