// PRNG function
inline float rand(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

// hsl2rgb
fixed3 hsl2rgb(float h, float s, float l) {
    float c = (1.0 - abs(2.0 * l - 1.0)) * s;
    float x = c * (1.0 - abs(fmod(h / 60.0, 2.0) - 1.0));
    float m = l - c / 2.0;
    fixed3 rgb = (fixed3)0;
    
    if (0.0 <= h && h < 60.0) {
        rgb = fixed3(c, x, 0.0);
    } else if (60.0 <= h && h < 120.0) {
        rgb = fixed3(x, c, 0.0);
    } else if (120.0 <= h && h < 180.0) {
        rgb = fixed3(0.0, c, x);
    } else if (180.0 <= h && h < 240.0) {
        rgb = fixed3(0.0, x, c);
    } else if (240.0 <= h && h < 300.0) {
        rgb = fixed3(x, 0.0, c);
    } else {
        rgb = fixed3(c, 0.0, x);
    }
    
    return rgb + fixed3(m, m, m);
}

// interpolate rainbow color with a normalized value
fixed3 interpolateRainbowColor(float value) {
    // Normalize the value to range [0, 1]
    float t = value;
    
    // Map the normalized value to a hue in the range [0, 300] (red to purple)
    float hue = 300.0 * t;
    
    // Use full saturation and lightness for vivid colors
    float saturation = 1.0;
    float lightness = 0.5;
    
    return hsl2rgb(hue, saturation, lightness);
}