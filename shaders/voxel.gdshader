shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_disabled;
uniform sampler2DArray block_textures : source_color, filter_nearest, repeat_enable;

void fragment() {
	vec4 albedo_tex = texture(block_textures, vec3(UV, UV2.x));
	ALBEDO = albedo_tex.rgb;
}
