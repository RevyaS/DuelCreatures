shader_type canvas_item;

uniform float heightReductionTop;
uniform float heightReductionBottom;
void fragment() {
    // Clip past path ends
    if (heightReductionTop != 0.f && UV.x > (1.f - heightReductionTop)) {
        discard;
    }
	if (heightReductionBottom != 0.f && UV.x < heightReductionBottom) {
        discard;
    }

    COLOR = texture(TEXTURE, UV);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
