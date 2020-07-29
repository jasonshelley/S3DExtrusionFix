# S3DExtrusionFix
Workaround for the dreaded "Move exceeds maximum extrusion" error which is caused by a bug in Simplify 3D.

Under some unknown conditions, just before a change in travel distance (starting on a long wall from a corner for instance), Simplify3D mistakenly places the change in extrusion on the line before it is needed.

This app corrects this using the following process:
1. The file is parsed top to bottom, capturing certain configuration parameters along the way.
2. For a travel movement with extrusion, the actual line width is calculated using the layer height from the configuration, and travel distance from previous position.
3. If that line width is greater than the larger of the configured line width * (first layer line width or single extrusion line width percentages), then the extrusion for that line is modified to output a line width of the indicated width. This can result in a cascade of modifications.

# Usage

S3DExtrusionFix path

# Happy Printing
