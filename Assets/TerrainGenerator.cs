using UnityEngine;
using System.Collections;
using System.Linq;

public class TerrainGenerator : MonoBehaviour {

	Terrain terrain;
	TerrainData tData;

	float [,] heights;
	int xRes;
	int yRes;

	void Start () {
		// Get the attached terrain component
		terrain = GetComponent<Terrain>();
		

		tData = terrain.terrainData;
		xRes = tData.heightmapWidth;
		yRes = tData.heightmapHeight;
		Debug.Log ("xRes: " + xRes);
		Debug.Log ("yRes: " + yRes);

//		Randomize ();
		DiamondSquare ();
		SetHeights ();
		AssignSplatMap ();
	}


	void Randomize () {
		heights = tData.GetHeights(0, 0, xRes, yRes);
		
		for (int y = 0; y < yRes; y += 1) {
			for (int x = 0; x < xRes; x += 1) {
				heights[x,y] = Random.Range(0f, 0.1f);
			}
		}
	}

	void DiamondSquare() {
		heights = tData.GetHeights(0, 0, xRes, yRes);
		float dis = 1f;

		Debug.Log("Diamond Square: " + dis);

		for (int x = 0; x < xRes; x += 1) {
			for (int y = 0; y < yRes; y += 1) {
				heights[x,y] = dis;
			}
		}

		heights[0,0] = RandD(dis);
		heights[xRes - 1,0] = RandD(dis);
		heights[0,yRes - 1] = RandD(dis);
		heights[xRes - 1, yRes - 1] = RandD(dis);


		// half-step of the square, gets divided immediately.
		int hs = xRes - 1;
		float roughness = 1.2f;

		do {
			hs = hs / 2;

			Debug.Log("HALF STEP: " + hs);

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
		float curHeight;
		float total = 0f;
		float max = heights [0, 0];
		float min = heights [0, 0];
		for (int y = 0; y < yRes; y++) {
			for (int x = 0; x < xRes; x++) {
				curHeight = heights[x, y];
				total += curHeight;
				if (curHeight > max)
					max = curHeight;
				if (curHeight < min)
					min = curHeight;
			}
		}
		Debug.Log ("Average: " + (total / (xRes * yRes)));
		Debug.Log ("Min: " + min);
		Debug.Log ("Max: " + max);

		float adjust = 1f / (max - min);
		Debug.Log ("Adjust: " + adjust);
		for (int y = 0; y < yRes; y++) {
			for (int x = 0; x < xRes; x++) {
				heights[x,y] = (heights[x,y] - min) * adjust;
			}
		}

		total = 0f;
		max = heights [0, 0];
		min = heights [0, 0];
		for (int y = 0; y < yRes; y++) {
			for (int x = 0; x < xRes; x++) {
				curHeight = heights[x, y];
				total += curHeight;
				if (curHeight > max)
					max = curHeight;
				if (curHeight < min)
					min = curHeight;
			}
		}
		Debug.Log ("Average: " + (total / (xRes * yRes)));
		Debug.Log ("Min: " + min);
		Debug.Log ("Max: " + max);


		tData.SetHeights (0, 0, heights);
	}

	void SetSquarePoint (int x, int y, int hs, float dis) {
		float a = heights[x - hs, y - hs];
		float b = heights[x + hs, y - hs];
		float c = heights[x - hs, y + hs];
		float d = heights[x + hs, y + hs];
		heights[x, y] = ((a + b + c + d) / 4f) + RandD(dis);
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

		heights[x, y] = (sum / count) + RandD(dis);
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
				
				// Sample the height at this location (note GetHeight expects int coordinates corresponding to locations in the heightmap array)
				float height = terrainData.GetHeight(Mathf.RoundToInt(y_01 * terrainData.heightmapHeight),Mathf.RoundToInt(x_01 * terrainData.heightmapWidth) );
				
				// Calculate the normal of the terrain (note this is in normalised coordinates relative to the overall terrain dimensions)
				Vector3 normal = terrainData.GetInterpolatedNormal(y_01,x_01);
				
				// Calculate the steepness of the terrain
				float steepness = terrainData.GetSteepness(y_01,x_01);
				
				// Setup an array to record the mix of texture weights at this point
				float[] splatWeights = new float[terrainData.alphamapLayers];
				
				// CHANGE THE RULES BELOW TO SET THE WEIGHTS OF EACH TEXTURE ON WHATEVER RULES YOU WANT
				
				// Texture[0] has constant influence
				splatWeights[0] = 0.2f;
				
				// Texture[1] is stronger at lower altitudes
				splatWeights[1] = 0f; // Mathf.Clamp01((terrainData.heightmapHeight - height));
				
				// Texture[2] stronger on flatter terrain
				// Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
				// Subtract result from 1.0 to give greater weighting to flat surfaces
				splatWeights[2] = 1.0f - Mathf.Clamp01(steepness*steepness/(terrainData.heightmapHeight/5.0f));
				
				// Texture[3] increases with height but only on surfaces facing positive Z axis 
				splatWeights[3] = 0f; // height * Mathf.Clamp01(normal.z);
				
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
