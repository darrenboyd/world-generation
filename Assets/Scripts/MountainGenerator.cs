using UnityEngine;
using System.Collections;
using System.Linq;

/*
 * Generate a Mountain for use in a heightmap
 */
public class MountainGenerator
{

	public readonly int radius; 
	public float minHeight = 0f;
	public float maxHeight = 1f;

	private float[,] heights;
	private Vector2 peak;
	private int resolution;

	public MountainGenerator(int mntRadius)
	{
		radius = mntRadius;

		Debug.Log ("Mountain X*Y (R): " + radius + " * " + radius + " (" + radius + ")");
		heights = new float[(2*radius) + 1, (2*radius) + 1];

		peak = new Vector2 (radius, radius);
	}

	public void Generate () {
		resolution = Mathf.NextPowerOfTwo(radius*2);
		heights = new float[resolution + 1,resolution + 1];

		float dis = 0.2f;
		float roughness = 2f;

		// Set the outside corners to the bottom.
		heights[0, 0] = minHeight;
		heights[0, resolution] = minHeight;
		heights[resolution, 0] = minHeight;
		heights[resolution, resolution] = minHeight;

		int hs = resolution;
		hs /= 2;

		// First step gets done manually, to force the peak.
		// Center of the resolution is the max.
		heights[hs, hs] = maxHeight;
		// Diamond around the center is the min.
		heights[hs, 0] = minHeight;
		heights[hs, resolution] = minHeight;
		heights[0, hs] = minHeight;
		heights[resolution, hs] = minHeight;

		do {
			hs /= 2;

			for (int x = hs; x < resolution; x += (hs * 2)) {
				for (int y = hs; y < resolution; y += (hs * 2)) {
					SetSquarePoint(x, y, hs, dis);
				}
			}
			
			for (int x = hs; x < resolution; x += (hs * 2)) {
				for (int y = hs; y < resolution; y += (hs * 2)) {
					SetDiamondPoint(x - hs, y, hs, dis);
					SetDiamondPoint(x, y - hs, hs, dis);
					SetDiamondPoint(x + hs, y, hs, dis);
					SetDiamondPoint(x, y + hs, hs, dis);
				}
			}
			
			dis *= Mathf.Pow(2, -roughness);
		} while ( hs > 1 );

		// Reset the peak to not be pointy.
		SetSquarePoint(resolution / 2, resolution / 2, 1, 0f);

	}

	void SetSquarePoint (int x, int y, int hs, float dis) {
		float a = heights[x - hs, y - hs];
		float b = heights[x + hs, y - hs];
		float c = heights[x - hs, y + hs];
		float d = heights[x + hs, y + hs];
		DisplaceHeight(x, y, ((a + b + c + d) / 4f), dis);
	}
	
	void SetDiamondPoint (int x, int y, int hs, float dis) {
		float sum=0f;
		float count=0f;
		
		if (x - hs >= 0) {
			sum += heights[x - hs, y];
			count += 1f;
		}
		
		if (x + hs < resolution) {
			sum += heights[x + hs, y];
			count += 1f;
		}
		
		if (y - hs >= 0) {
			sum += heights[x, y - hs];
			count += 1f;
		}
		
		if (y + hs < resolution) {
			sum += heights[x, y + hs];
			count += 1f;
		}
		
		DisplaceHeight(x, y, (sum / count), dis);
	}

	private void DisplaceHeight(int x, int y, float avg, float dis)
	{
		heights[x, y] = Mathf.Clamp(avg + Random.Range(-dis, dis), minHeight, maxHeight);
	}


	public delegate void SetHeight(int x, int y, float height);

	public void ApplyHeights (int offsetX, int offsetY, SetHeight f)
	{
		int offset = (resolution - (radius * 2))  / 2;
		for (int x=0; x <= (radius * 2); x++) {
			for (int y=0; y <= (radius * 2); y++) {
				// Avoid the 'corners'
//				Vector2 pos = new Vector2 (x, y);
//				float distance = Mathf.Abs (Vector2.Distance (pos, peak));
//				if (distance > (float)radius) continue;
				f(offsetX + x, offsetY + y, heights[x + offset,y + offset]);
			}
		}
	}

}
