using UnityEngine;
using System.Collections;
using System.Linq;

public class TerrainGenerator : MonoBehaviour {

	Terrain terrain;
	TerrainData tData;

	float [,] heights;
	int xRes;
	int yRes;

	float minHeight;
	float maxHeight;

	void Start () {
		// Get the attached terrain component
		terrain = GetComponent<Terrain>();
		

		tData = terrain.terrainData;
		xRes = tData.heightmapWidth;
		yRes = tData.heightmapHeight;
		heights = tData.GetHeights(0, 0, xRes, yRes);
		Debug.Log ("X * Y: " + xRes + " * " + yRes + " == " + (xRes * yRes));

		Flatten ();
		Mountain ();
		DiamondSquare ();
		SetHeights ();
		DebugHeightmap();
		AssignSplatMap ();
	}

	void DebugHeightmap()
	{
		Debug.Log ("Height MIN: " + minHeight);
		Debug.Log ("Height MAX: " + maxHeight);
	}

	void SetHeight(int x, int y, float height)
	{
		heights[x,y] = height;
		if (height > maxHeight) maxHeight = height;
		if (height < minHeight) minHeight = height;
	}

	public delegate void HandleHeightmapPosition(int x, int y);

	void ForHeightmap(HandleHeightmapPosition f)
	{
		for (int y = 0; y < yRes; y += 1)
			for (int x = 0; x < xRes; x += 1)
				f(x, y);
	}

	void Flatten ()
	{
		ForHeightmap((x,y) => SetHeight(x, y, 0f));
	}

	void Shape () {
		ForHeightmap((x,y) => {
			Vector2 position = new Vector2((float)x / (float)xRes, (float)y / (float)yRes);
			SetHeight(x, y, Vector2.Distance(Vector2.zero, position));
		});
	}

	void Mountain ()
	{
		int x_p = (xRes / 3) * 2;
		int y_p = (yRes / 3) * 2;
		int radius = Mathf.Min(xRes, yRes) / 8;
		Debug.Log("Mountain X: " + x_p);
		Debug.Log("Mountain Y: " + y_p);
		Debug.Log("Mountain R: " + radius);

		Vector2 peak = new Vector2 (x_p, y_p);
		Vector2 corner = new Vector2(x_p - radius, y_p - radius);

		float maxDistance = Vector2.Distance(peak, corner);

		int limit = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(radius, 2) * 2));

		for (int x= x_p - limit; x <= x_p + limit; x++)
		{
			for (int y= y_p - limit; y <= y_p + limit; y++)
			{
				Vector2 pos = new Vector2(x, y);
				float distance = Mathf.Abs(Vector2.Distance (pos, peak));
				if (distance > maxDistance)
					continue;
				float max = 1f - (distance / maxDistance);
				float min = max * Random.Range (0.7f, 1.05f);
				if (min > max)
					SetHeight(x, y, min);
				else
					SetHeight(x, y, Random.Range (min, max));
			}
		}

	}

	void DiamondSquare() {
		heights = tData.GetHeights(0, 0, xRes, yRes);
		float dis = 1f;

		// half-step of the square, gets divided immediately.
		int hs = xRes - 1;
		float roughness = 1.2f;

		do {
			hs = hs / 2;

			for (int x = hs; x < xRes; x += (hs * 2)) {
				for (int y = hs; y < yRes; y += (hs * 2)) {
					SetSquarePoint(x, y, hs, dis);
				}
			}

			for (int x = hs; x < xRes; x += (hs * 2)) {
				for (int y = hs; y < yRes; y += (hs * 2)) {
					SetDiamondPoint(x - hs, y, hs, dis);
					SetDiamondPoint(x, y - hs, hs, dis);
					SetDiamondPoint(x + hs, y, hs, dis);
					SetDiamondPoint(x, y + hs, hs, dis);
				}
			}

			dis *= Mathf.Pow(2, -roughness);
		} while (hs > 1);
	}

	// return a random value between -d and d
	float RandD(float d) {
		return ((Random.value * 2f) - 1f) * d;
	}

	void SetHeights ()
	{
		tData.SetHeights (0, 0, heights);
	}

	void SetSquarePoint (int x, int y, int hs, float dis) {
		float a = heights[x - hs, y - hs];
		float b = heights[x + hs, y - hs];
		float c = heights[x - hs, y + hs];
		float d = heights[x + hs, y + hs];
		SetHeight(x, y, ((a + b + c + d) / 4f) + RandD(dis));
	}

	void SetDiamondPoint (int x, int y, int hs, float dis) {
		float sum=0f;
		float count=0f;

		if (x - hs >= 0) {
			sum += heights[x - hs, y];
			count += 1f;
		}

		if (x + hs < xRes) {
			sum += heights[x + hs, y];
			count += 1f;
		}

		if (y - hs >= 0) {
			sum += heights[x, y - hs];
			count += 1f;
		}
		
		if (y + hs < yRes) {
			sum += heights[x, y + hs];
			count += 1f;
		}

		SetHeight(x, y, (sum / count) + RandD(dis));
	}

	static float normalizedHeight (TerrainData terrainData, float y_01, float x_01)
	{
		// Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
		int y_coord = Mathf.RoundToInt (y_01 * terrainData.heightmapHeight);
		var x_coord = Mathf.RoundToInt (x_01 * terrainData.heightmapWidth);
		// GetHeight returns your Terrain Height, which will be between 0 and N (set in Unity)
		float height = terrainData.GetHeight (y_coord, x_coord);
		return height / terrainData.size.y;
	}

	float normalizedSteepness(TerrainData terrainData, float y, float x)
	{
		float steepness = terrainData.GetSteepness(y,x);
		steepness = 1 / (steepness + 1f);
		return 1f - steepness;
	}

	void AssignSplatMap () {
		// Get a reference to the terrain data
		TerrainData terrainData = terrain.terrainData;
		
		// Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
		float[, ,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];


		float min, max;		
		min = normalizedSteepness(terrainData, 0, 0);
		max = min;

		for (int y = 0; y < terrainData.alphamapHeight; y++)
		{
			for (int x = 0; x < terrainData.alphamapWidth; x++)
			{
				// Normalise x/y coordinates to range 0-1 
				float y_01 = (float)y/(float)terrainData.alphamapHeight;
				float x_01 = (float)x/(float)terrainData.alphamapWidth;
				
				// Setup an array to record the mix of texture weights at this point
				float[] splatWeights = new float[terrainData.alphamapLayers];
				
				float height = normalizedHeight (terrainData, y_01, x_01);

				float steepness = normalizedSteepness(terrainData, y_01,x_01);
				if (steepness < min) min = steepness;
				if (steepness > max) max = steepness;

				
				// Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
				// Vector3 normal = terrainData.GetInterpolatedNormal(y_01,x_01);

				// Dirt is more prevalent with height and flatness
				splatWeights[1] = height * (1f - steepness);

				// The grass is stronger at lower altitudes
				splatWeights[0] = (1f - Mathf.Pow (height, 3f));

				// Cliff is stronger when steep
				splatWeights[2] = steepness;

				// Texture[3] increases with height but only on surfaces facing positive Z axis 
				// splatWeights[3] = 0f; // height * Mathf.Clamp01(normal.z);

				// Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
				float z = splatWeights.Sum();
				
				// Loop through each terrain texture
				for(int i = 0; i<terrainData.alphamapLayers; i++){
					
					// Normalize so that sum of all texture weights = 1
					splatWeights[i] /= z;
					
					// Assign this point to the splatmap array
					splatmapData[x, y, i] = splatWeights[i];
				}
			}
		}

		Debug.Log ("MIN: " + min);
		Debug.Log ("MAX: " + max);

//		for (int i=0; i< 10; i++)
//		{
//			Debug.Log ("Clamp01: " + Mathf.Clamp01( Random.Range(min + 1f, max + 1f)));
//		}

		// Finally assign the new splatmap to the terrainData:
		terrainData.SetAlphamaps(0, 0, splatmapData);
	}

}
