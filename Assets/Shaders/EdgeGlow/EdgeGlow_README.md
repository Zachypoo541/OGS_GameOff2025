# Edge Glow Shader with Chromatic Aberration

## Installation

1. Copy `EdgeGlowShader.shader` into your Unity project's Assets folder (anywhere in Assets will work)
2. Create a new Material in Unity
3. Select the shader: Custom/EdgeGlowWithChromaticAberration
4. Apply the material to your objects

## Adjustable Parameters

### Base Appearance
- **Base Color (Black)**: The main color of your object (default: pure black)
- **Edge Color (White)**: The color of the glowing edges (default: pure white)
- **Edge Thickness**: How thick the white border appears (0-1, default: 0.2)
- **Edge Brightness**: Multiplier for the edge glow intensity (0-5, default: 2.0)

### Edge Detection Methods
- **Fresnel Power**: Controls how the Fresnel effect detects edges (0.1-10, default: 3.0)
  - Higher values = sharper, thinner edges
  - Lower values = softer, wider edges
  
- **Normal Edge Power**: Controls normal-based edge detection (0.1-10, default: 2.0)
  - Higher values = more pronounced edges on perpendicular faces
  - Lower values = softer edge detection
  
- **Edge Detection Blend**: Blends between Fresnel (0) and Normal-based (1) detection (0-1, default: 0.5)
  - 0 = Only Fresnel (edges based on viewing angle)
  - 1 = Only Normal-based (edges on perpendicular faces)
  - 0.5 = Equal mix of both methods

### Chromatic Aberration
- **Chromatic Aberration Intensity**: How far the RGB channels spread (0-0.1, default: 0.02)
  - Higher values = more visible color separation
  - Lower values = subtle effect
  
- **Chromatic Aberration Falloff**: Controls the gradient of the chromatic effect (0.1-5, default: 2.0)
  - Higher values = sharper color transitions
  - Lower values = softer, more gradual color spread

## Tips for Tweaking

1. **For sharper, more defined edges**: Increase Fresnel Power and Normal Edge Power
2. **For softer, glowing edges**: Decrease the power values and increase Edge Thickness
3. **For more pronounced chromatic aberration**: Increase Chromatic Aberration Intensity
4. **To favor one detection method**: Adjust Edge Detection Blend toward 0 (Fresnel) or 1 (Normal)
5. **For very dark gray instead of black**: Adjust Base Color to something like (0.05, 0.05, 0.05, 1)

## How It Works

The shader combines two edge detection methods:
1. **Fresnel Effect**: Detects edges based on the angle between the surface normal and view direction
2. **Normal-Based**: Detects faces that are perpendicular to the camera

The chromatic aberration spreads outward by offsetting the red, green, and blue channels at different distances from the edge, creating a rainbow-like effect on the border's outer edge.

## Performance

This shader is optimized for performance with:
- Direct HLSL code (no Shader Graph overhead)
- Minimal texture lookups (none)
- Efficient per-fragment calculations
- Proper shadow casting and depth passes for URP

## Troubleshooting

- **Edges not showing**: Increase Edge Thickness or decrease the power values
- **Chromatic aberration not visible**: Increase Chromatic Aberration Intensity
- **Effect too subtle**: Increase Edge Brightness
- **Effect too harsh**: Decrease Edge Brightness and increase Edge Thickness for a softer look
