#version 330 core

in vec4 vColor;
in vec2 vTexCoord;

uniform sampler2D uTexture;
uniform int uUseTexture;
uniform vec2 uTextureSize;

out vec4 FragColor;

void main()
{
    vec4 sampled = vec4(1.0);

    if (uUseTexture != 0)
    {
        vec2 safeSize = max(uTextureSize, vec2(1.0));
        sampled = texture(uTexture, vTexCoord / safeSize);
    }

    FragColor = sampled * vColor;
}
