# S3DExtrusionFix
Workaround for the dreaded "Move exceeds maximum extrusion" error which is caused by a bug in Simplify 3D.

Under some unknown conditions, just before a change in travel distance (starting on a long wall from a corner for instance), Simplify3D mistakenly places the change in extrusion on the line before it is needed.

This app corrects this using the following process:
1. For each travel movement that includes extrusion, calculate the distance travelled and the extrusion to distance ratio (d, e/d);
2. Identify any outliers that have an extrusion ratio more than the mean + one standard deviation.
3. For those outliers, replace the extrusion value with the previous distance + the distance (d) * mean extrusion ratio (e/d).
4. Write out a new gcode file with these lines fixed to a file called <previous filename>.fixed.gcode

# Usage

S3DExtrusionFix <path>
