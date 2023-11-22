extends MeshInstance3D

# Called when the node enters the scene tree for the first time.
func _ready():
	var st = SurfaceTool.new()
	
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	st.set_color(Color(1, 1, 1))
	
	st.set_uv(Vector2(0, 0))
	st.add_vertex(Vector3(0, 0, 0))
	st.set_uv(Vector2(1, 0))
	st.add_vertex(Vector3(1, 0, 0))
	st.set_uv(Vector2(1, 1))
	st.add_vertex(Vector3(1, 0, 1))
	
	st.set_uv(Vector2(0, 0))
	st.add_vertex(Vector3(0, 0, 0))
	st.set_uv(Vector2(1, 1))
	st.add_vertex(Vector3(1, 0, 1))
	st.set_uv(Vector2(0, 1))
	st.add_vertex(Vector3(0, 0, 1))
	
	print("AAA")
	st.generate_normals()
	mesh = st.commit()
	
	material_override = StandardMaterial3D.new()
	material_override.albedo_texture = load("res://stone.png")
	material_override.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
