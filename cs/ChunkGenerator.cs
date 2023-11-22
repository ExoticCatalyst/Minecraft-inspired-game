using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkGenerator : MeshInstance3D
{
	private static void AddVertexData(List<Vector2> uvs, List<Vector3> normals, List<int> indices, ref int i, Vector3 normal)
	{
		uvs.Add(new Vector2(0, 0));
		uvs.Add(new Vector2(1, 0));
		uvs.Add(new Vector2(1, 1));
		uvs.Add(new Vector2(0, 1));
		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);
		indices.Add(i + 0);
		indices.Add(i + 1);
		indices.Add(i + 2);
		indices.Add(i + 0);
		indices.Add(i + 2);
		indices.Add(i + 3);
		i += 4;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();

		int indicesIndex = 0;

		for (int i = 0; i < 5; i++)
		{
			Vector3I blockPos = new(0, i, 0);

			// top face
			verts.Add(new Vector3(0, 1, 0) + blockPos);
			verts.Add(new Vector3(1, 1, 0) + blockPos);
			verts.Add(new Vector3(1, 1, 1) + blockPos);
			verts.Add(new Vector3(0, 1, 1) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 1, 0));

			// bottom face
			verts.Add(new Vector3(0, 0, 1) + blockPos);
			verts.Add(new Vector3(1, 0, 1) + blockPos);
			verts.Add(new Vector3(1, 0, 0) + blockPos);
			verts.Add(new Vector3(0, 0, 0) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, -1, 0));

			// right face
			verts.Add(new Vector3(1, 0, 0) + blockPos);
			verts.Add(new Vector3(1, 0, 1) + blockPos);
			verts.Add(new Vector3(1, 1, 1) + blockPos);
			verts.Add(new Vector3(1, 1, 0) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(1, 0, 0));

			// left face
			verts.Add(new Vector3(0, 1, 0) + blockPos);
			verts.Add(new Vector3(0, 1, 1) + blockPos);
			verts.Add(new Vector3(0, 0, 1) + blockPos);
			verts.Add(new Vector3(0, 0, 0) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(-1, 0, 0));

			// back face
			verts.Add(new Vector3(0, 0, 0) + blockPos);
			verts.Add(new Vector3(1, 0, 0) + blockPos);
			verts.Add(new Vector3(1, 1, 0) + blockPos);
			verts.Add(new Vector3(0, 1, 0) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 0, -1));

			// front face
			verts.Add(new Vector3(0, 1, 1) + blockPos);
			verts.Add(new Vector3(1, 1, 1) + blockPos);
			verts.Add(new Vector3(1, 0, 1) + blockPos);
			verts.Add(new Vector3(0, 0, 1) + blockPos);
			AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 0, 1));
		}
		
		// convert lists to arrays and assign to surface array
		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		Mesh = arrayMesh;

		var material = new StandardMaterial3D();
		MaterialOverride = material;
		material.AlbedoTexture = GD.Load("res://stone.png") as Texture2D;
		material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
		material.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
