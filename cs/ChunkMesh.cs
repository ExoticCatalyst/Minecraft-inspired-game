using Godot;
using System.Collections;
using System.Collections.Generic;

namespace Voxel
{
    public partial class ChunkMesh : MeshInstance3D
    {
        private VoxelWorld world;
        private int cx, cz;
        private ArrayMesh arrayMesh;
        private ConcavePolygonShape3D collisionShape;

        public Vector2I ChunkPos { get => new(cx, cz); }

        public ChunkMesh(VoxelWorld world, int cx, int cz)
        {
            this.cx = cx;
            this.cz = cz;
            this.world = world;

            Name = $"Chunk {cx}, {cz}";
            Position = new Vector3(cx * ChunkData.SIZE_X, 0, cz * ChunkData.SIZE_Z);
        }

        public override void _Ready()
        {
            base._Ready();

            arrayMesh = new ArrayMesh();
            var material = new StandardMaterial3D() {
                AlbedoTexture = GD.Load("res://Textures/stone.png") as Texture2D,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled
            };

            var staticBody = new StaticBody3D();
            collisionShape = new ConcavePolygonShape3D();
            var shapeNode = new CollisionShape3D() {
                Shape = collisionShape
            };

            Mesh = arrayMesh;
            MaterialOverride = material;
            staticBody.AddChild(shapeNode);
            AddChild(staticBody);

            GenerateMesh();
        }

        // Return true if a given block id is transparent
        private static bool IsTransparent(int blockId)
        {
            return blockId == 0;
        }

        public void GenerateMesh()
        {
            if (arrayMesh == null || collisionShape == null)
            {
                throw new System.NullReferenceException("ChunkMesh is not ready");
            }

            const int CHUNK_WIDTH = ChunkData.SIZE_X;
			const int CHUNK_HEIGHT = ChunkData.SIZE_Y;
			const int CHUNK_DEPTH = ChunkData.SIZE_Z;

            var startTime = Time.GetTicksMsec();

            var chunkData = world.GetChunk(cx, cz);

            var surfaceArray = new Godot.Collections.Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var indices = new List<int>();

            int indicesIndex = 0;

            // helper function
            void addVertexData(Vector3 normal)
            {
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                indices.Add(indicesIndex + 0);
                indices.Add(indicesIndex + 1);
                indices.Add(indicesIndex + 2);
                indices.Add(indicesIndex + 0);
                indices.Add(indicesIndex + 2);
                indices.Add(indicesIndex + 3);
                indicesIndex += 4;
            }

            // get voxels in neighbor chunk
            var neighborChunks = new ChunkData[4];
            world.TryGetChunk(cx-1, cz, out neighborChunks[0]);
            world.TryGetChunk(cx+1, cz, out neighborChunks[1]);
            world.TryGetChunk(cx, cz-1, out neighborChunks[2]);
            world.TryGetChunk(cx, cz+1, out neighborChunks[3]);
            
            bool isTransparent(int x, int y, int z)
            {
                if (x < 0 && neighborChunks[0] != null)
                    return IsTransparent(neighborChunks[0].Get(x + CHUNK_WIDTH, y, z));

                else if (x >= CHUNK_WIDTH && neighborChunks[1] != null)
                    return IsTransparent(neighborChunks[1].Get(x - CHUNK_WIDTH, y, z));

                if (z < 0 && neighborChunks[2] != null)
                    return IsTransparent(neighborChunks[2].Get(x, y, z + CHUNK_DEPTH));

                else if (z >= CHUNK_DEPTH && neighborChunks[3] != null)
                    return IsTransparent(neighborChunks[3].Get(x, y, z - CHUNK_DEPTH));

                return IsTransparent(chunkData.Get(x, y, z));
            };

            // loop through all blocks in the chunk
            // to generate mesh data
            for (int x = 0; x < CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < CHUNK_DEPTH; z++)
                    {
                        // air is invisible
                        if (chunkData.Get(x, y, z) == 0) continue;

                        Vector3I blockPos = new(x, y, z);

                        // top face
                        if (isTransparent(x, y+1, z))
                        {
                            verts.Add(new Vector3(0, 1, 0) + blockPos);
                            verts.Add(new Vector3(1, 1, 0) + blockPos);
                            verts.Add(new Vector3(1, 1, 1) + blockPos);
                            verts.Add(new Vector3(0, 1, 1) + blockPos);
                            addVertexData(new Vector3(0, 1, 0));
                        }

                        // bottom face
                        if (isTransparent(x, y-1, z))
                        {
                            verts.Add(new Vector3(0, 0, 1) + blockPos);
                            verts.Add(new Vector3(1, 0, 1) + blockPos);
                            verts.Add(new Vector3(1, 0, 0) + blockPos);
                            verts.Add(new Vector3(0, 0, 0) + blockPos);
                            addVertexData(new Vector3(0, -1, 0));
                        }

                        // right face
                        if (isTransparent(x+1, y, z))
                        {
                            verts.Add(new Vector3(1, 0, 0) + blockPos);
                            verts.Add(new Vector3(1, 0, 1) + blockPos);
                            verts.Add(new Vector3(1, 1, 1) + blockPos);
                            verts.Add(new Vector3(1, 1, 0) + blockPos);
                            addVertexData(new Vector3(1, 0, 0));
                        }

                        // left face
                        if (isTransparent(x-1, y, z))
                        {
                            verts.Add(new Vector3(0, 1, 0) + blockPos);
                            verts.Add(new Vector3(0, 1, 1) + blockPos);
                            verts.Add(new Vector3(0, 0, 1) + blockPos);
                            verts.Add(new Vector3(0, 0, 0) + blockPos);
                            addVertexData(new Vector3(-1, 0, 0));
                        }

                        // back face
                        if (isTransparent(x, y, z-1))
                        {
                            verts.Add(new Vector3(0, 0, 0) + blockPos);
                            verts.Add(new Vector3(1, 0, 0) + blockPos);
                            verts.Add(new Vector3(1, 1, 0) + blockPos);
                            verts.Add(new Vector3(0, 1, 0) + blockPos);
                            addVertexData(new Vector3(0, 0, -1));
                        }

                        // front face
                        if (isTransparent(x, y, z+1))
                        {
                            verts.Add(new Vector3(0, 1, 1) + blockPos);
                            verts.Add(new Vector3(1, 1, 1) + blockPos);
                            verts.Add(new Vector3(1, 0, 1) + blockPos);
                            verts.Add(new Vector3(0, 0, 1) + blockPos);
                            addVertexData(new Vector3(0, 0, 1));
                        }
                    }
                }
            }

            // convert lists to arrays and assign to surface array
            GD.Print(verts.Count);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

            // finalize mesh
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
            GD.Print($"chunk mesh took {Time.GetTicksMsec() - startTime} ms");

            var collisionPolygon = new Vector3[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                collisionPolygon[i] = verts[indices[i]];
            }

            collisionShape.Data = collisionPolygon;
        }
    }
}