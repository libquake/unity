using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LibQuake
{
    /// <summary>
    /// Turns libquake brush entities into Unity meshes. libquake works in Quake coordinates, see QuakeConvert for
    /// the mapping to Unity. The mapping mirrors, which turns Quake's clockwise front faces into Unity's clockwise
    /// front faces, so indices stay as they are.
    /// </summary>
    public class SolidEntityGenerator
    {
        private readonly MapConfigData _configData;

        public SolidEntityGenerator(MapConfigData configData)
        {
            _configData = configData;
        }

        private static Vector3 ToUnityDirection(float x, float y, float z) => QuakeConvert.ToUnityDirection(x, y, z);

        private Vector3 ToUnityPosition(float x, float y, float z) =>
            QuakeConvert.ToUnityPosition(x, y, z, _configData.inverseScale);

        /// <summary>
        /// One mesh with a submesh per visible texture. Clip, skip and nodraw surfaces are left out.
        /// Returns null if nothing is visible.
        /// </summary>
        public Mesh GenerateRenderMesh(MapSolidEntity entity, string name, Func<string, Material> getMaterial,
            out Material[] materials)
        {
            var visible = new List<MapSubmesh>();
            int vertexCount = 0;
            foreach (var s in entity.Submeshes)
            {
                if (s.SurfaceType != SurfaceType.SOLID || s.IndexCount == 0)
                    continue;
                visible.Add(s);
                vertexCount += (int)s.VertexCount;
            }

            materials = new Material[visible.Count];
            if (visible.Count == 0)
                return null;

            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];
            var uvs = new Vector2[vertexCount];
            var lightmapUVs = new Vector2[vertexCount];
            var triangles = new List<int[]>(visible.Count);

            int offset = 0;
            for (int s = 0; s < visible.Count; s++)
            {
                var sub = visible[s];
                for (int i = 0; i < sub.VertexCount; i++)
                {
                    ref var v = ref entity.Vertices[sub.VertexOffset + i];
                    int o = offset + i;
                    positions[o] = ToUnityPosition(v.pos.x, v.pos.y, v.pos.z);
                    normals[o] = ToUnityDirection(v.normal.x, v.normal.y, v.normal.z);
                    // the mirror flips the bitangent and so does the flipped v below, w stays
                    var t = ToUnityDirection(v.tangent.x, v.tangent.y, v.tangent.z);
                    tangents[o] = new Vector4(t.x, t.y, t.z, v.tangent.w);
                    uvs[o] = new Vector2(v.uv.x, -v.uv.y);
                    // lightmap rows are uploaded as they come, row 0 is v = 0 in Unity as well
                    lightmapUVs[o] = new Vector2(v.lightmapUV.x, v.lightmapUV.y);
                }

                var tris = new int[sub.IndexCount];
                for (int i = 0; i < tris.Length; i++)
                    tris[i] = (int)(entity.Indices[sub.IndexOffset + i] - sub.VertexOffset) + offset;
                triangles.Add(tris);

                materials[s] = getMaterial(sub.TextureName);
                offset += (int)sub.VertexCount;
            }

            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uvs;
            mesh.uv2 = lightmapUVs;
            mesh.subMeshCount = triangles.Count;
            for (int s = 0; s < triangles.Count; s++)
                mesh.SetTriangles(triangles[s], s, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// All hulls of the given types merged into one mesh, for a static (non convex) MeshCollider.
        /// </summary>
        public Mesh GenerateCollisionMesh(MapSolidEntity entity, string name, Predicate<HullType> include)
        {
            var points = new List<Vector3>();
            var indices = new List<int>();
            foreach (var hull in entity.Hulls)
            {
                if (!include(hull.Type))
                    continue;
                int baseIndex = points.Count;
                foreach (var p in hull.Points)
                    points.Add(ToUnityPosition(p.x, p.y, p.z));
                foreach (var i in hull.Indices)
                    indices.Add(baseIndex + i);
            }

            if (indices.Count == 0)
                return null;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(points);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// One mesh per hull, for convex colliders (triggers, liquids, moving brushes).
        /// </summary>
        public List<Mesh> GenerateHullMeshes(MapSolidEntity entity, string name, Predicate<HullType> include)
        {
            var meshes = new List<Mesh>();
            foreach (var hull in entity.Hulls)
            {
                if (!include(hull.Type))
                    continue;
                var points = new Vector3[hull.Points.Length];
                for (int i = 0; i < points.Length; i++)
                    points[i] = ToUnityPosition(hull.Points[i].x, hull.Points[i].y, hull.Points[i].z);
                var mesh = new Mesh { name = $"{name}_{meshes.Count}" };
                mesh.vertices = points;
                mesh.triangles = hull.Indices;
                mesh.RecalculateBounds();
                meshes.Add(mesh);
            }

            return meshes;
        }

        /// <summary>
        /// An instance of the prefab configured for the entity's class, the trigger prefab for trigger_* entities
        /// or the default solid prefab, with the entity's attributes applied.
        /// </summary>
        public GameObject GetGameObjectForSolidEntity(MapSolidEntity entity)
        {
            var prefab = FindPrefab(entity.ClassName);
            if (prefab == null)
                throw new InvalidOperationException($"No prefab for solid entity '{entity.ClassName}', check the " +
                                                    "default objects of the map config.");

            var obj = Object.Instantiate(prefab);
            SolidEntity.SetupPrefab(obj, entity.Attributes, _configData.inverseScale);
            return obj;
        }

        private GameObject FindPrefab(string className)
        {
            foreach (var entry in _configData.solidEntities)
            {
                if (entry.className == className && entry.prefab != null)
                    return entry.prefab;
            }

            return className.Contains("trigger") ? _configData.defaultTriggerObject : _configData.defaultSolidObject;
        }
    }
}
