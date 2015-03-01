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

	public float groundMinHeight = 0.1f;

	void Start () {
		// Get the attached terrain component
		terrain = GetComponent<Terrain>();
		

		tData = terrain.terrainData;
		xRes = tData.heightmapWidth;
		yRes = tData.heightmapHeight;
		heights = tData.GetHeights(0, 0, xRes, yRes);
		maxHeight = heights[0,0];
		minHeight = maxHeight;

		Debug.Log ("X * Y: " + xRes + " * " + yRes + " == " + (xRes * yRes));

		Flatten ();
		DebugHeightmap("After Flatten");
		GenerateMountains ();
		DebugHeightmap("After Mountain");
		DiamondSquare ();
		DebugHeightmap("After Diamond Square");

		tData.SetHeights (0, 0, heights);

		AssignSplatMap ();
	}

	void DebugHeightmap(string title)
	{
		Debug.Log (title);
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
		ForHeightmap((x,y) => {
			int fromX = Mathf.Min (x, xRes - 1 - x);
			int fromY = Mathf.Min (y, yRes - 1 - y);
			int dist = Mathf.Min (fromX, fromY) + 1;
			SetHeight(x, y, (1f - (1f / (Mathf.Pow((dist / 2f) + 1f, 2f)))) * 0.1f);
		});
	}

	// Rise with distance from origin
	void Shape () {
		ForHeightmap((x,y) => {
			Vector2 position = new Vector2((float)x / (float)xRes, (float)y / (float)yRes);
			SetHeight(x, y, Vector2.Distance(Vector2.zero, position));
		});
	}

	void GenerateMountains ()
	{
		int x_p = (xRes / 3) * 2;
		int y_p = (yRes / 3) * 2;
		int radius = Mathf.Min(xRes, yRes) / 8;
		BuildMountain (x_p, y_p, radius);
		BuildMountain (x_p - radius, y_p, radius);
		BuildMountain (x_p - (radius * 2), y_p - radius, radius);

	}
	

	void BuildMountain (int xPeak, int yPeak, int radius)
	{
		Debug.Log ("Mountain X*Y (R): " + xPeak + " * " + yPeak + " (" + radius + ")");

		Vector2 peak = new Vector2 (xPeak, yPeak);
		float maxHeight = 1f;
		float minHeight = groundMinHeight;
		float totalGrade = maxHeight - minHeight;

		for (int dist=0; dist <= radius; dist++) {
			for (int x = xPeak - dist; x <= xPeak + dist; x++) {
				for (int y = yPeak - dist; y <= yPeak + dist; y++) {
					// Avoid touching the 'corners' to allow a circular base
					Vector2 pos = new Vector2 (x, y);
					float distance = Mathf.Abs (Vector2.Distance (pos, peak));
					if (distance > (float)radius) continue;

					float parabolaFactor = 1f - Mathf.Pow (distance / (float) radius, 2f);
					float coanFactor     = 1f - (distance / (float) radius);
					float factor = (parabolaFactor + coanFactor) / 2f;
					float height = (totalGrade * factor) + minHeight;
					Random.Range

					SetHeight (x, y, Mathf.Max (height, heights[x,y]));
				}
			}
		}
	}

	void DiamondSquare() {

		float dis = 0.8f;
		float roughness = 1f;
		int hs = xRes - 1;
		hs /= 2;

		do {
			hs /= 2;

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
		} while ( hs > 1 );
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

		DisplaceHeight(x, y, (sum / count), dis);
	}

	void DisplaceHeight(int x, int y, float avg, float dis)
	{
//		float curHeight = heights[x, y];
//		float newHeight = Mathf.Clamp01(avg + Random.Range(-dis, dis));
//		newHeight = ((curHeight * 4f) + newHeight) / 5f;
//		SetHeight(x, y, curHeight);
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

				// Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
				// Vector3 normal = terrainData.GetInterpolatedNormal(y_01,x_01);

				if (height < 0.09f) {
					splatWeights[3] = 1f;
				} else if (height > 0.8f) {
					splatWeights[2] = steepness * height;
					splatWeights[4] = steepness;
				} else {
					// The default dirt is constant
					splatWeights[0] = 0.1f;
					
					// The grass is stronger at lower altitudes
					splatWeights[1] = (1f - Mathf.Pow (height, 2f));
					
					// Dirt is stronger at high altitudes.
					splatWeights[2] = height;

					// Texture[3] increases with height but only on surfaces facing positive Z axis 
					// splatWeights[3] = 0f; // height * Mathf.Clamp01(normal.z);
					
				}


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

		// Finally assign the new splatmap to the terrainData:
		terrainData.SetAlphamaps(0, 0, splatmapData);
	}

}
