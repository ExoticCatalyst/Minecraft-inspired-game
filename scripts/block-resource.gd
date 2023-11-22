extends Resource

@export var texture: Texture2D
@export var side_texture: Texture2D
@export var top_texture: Texture2D
@export var bottom_texture: Texture2D

func _init(p_texture = null, p_side_texture = null, p_top_texture = null, p_bottom_texture = null):
	texture = p_texture
	side_texture = p_side_texture
	top_texture = p_top_texture
	bottom_texture = p_bottom_texture
