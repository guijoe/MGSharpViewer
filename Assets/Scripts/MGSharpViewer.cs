using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MGSharp.Viewer
{
    public class MGSharpViewer : MonoBehaviour
    {
        public bool radialRays = false;
        public bool membraneRays = false;
        public bool adhesionRays = false;
        public bool nucleus = false;
        public bool particles = false;
		//public bool readAdhesions = true;
		Color[] tissueColors;
        
        [Range(0, 74)]
        public int particleDrawn = 0;

        int popSize;
        int maxPopSize;
        int nbOfCellTypes;
        int nbOfFrames;
        int nbOfLinesPerFrameStates;
        
        int[] nbOfCellsPerType;
        int[] nbOfVerticesPerType;
        int[][] nbOfVerticesPerTypePerCell;
        int[] nbOfVerticesPerCell;
        int[] nbOfTrianglesPerType;
        Mesh[] meshPerCellType;

        int[] popSizePerFrame;
        int[] startOfFrame;
        int[] nbOfCellTypesPerFrame;
        int[][] nbOfCellsPerTypePerFrame;
        int[][][] nbOfVerticesPerTypePerCellPerFrame;
        int[][] nbOfVerticesPerCellPerFrame;
        Vector3[][][][] positionsPerTypePerCellPerVerticePerFrame;

        List<Vector4>[] externalEdges;
        List<Vector4> currentExternalEdges;
        
        public MGCell cellPrefab;
        public MGTissue tissuePrefab;
        public MGParticle particlePrefab;
        MGCell[] cells;
		MGTissue[] tissues;
        Vector3[] positions;
        IList<PersistantVertex> states;
        Vector3[] numbers;
        int frameStartStates = 0;
        

        [Range(1, 10000)]
        public int frame;
        [Range(1, 100)]
        public int speed = 1;

        public string folder;
        public bool play = false;
        public bool reset = false;

        public bool drawDiffusion = false;
        public bool drawVectorField = false;
        public bool drawParticles = false;

        // Use this for initialization
        void Start()
        {
			tissueColors = new Color[]{ 
				new Color(153f / 256, 102f / 256, 0),
				Color.magenta,
				new Color(236f / 256, 236f / 256, 134f / 256)
			};


            Debug.Log("Reading parameters ...");
            ReadParameters();
            
            Debug.Log("Creating cell population ...");
            CreateCellPopulation();

            Debug.Log("Reading results ...");
            ReadResults();

            string neighbourhoodsFile = folder + "/InterCellEdges.mg";
            if (File.Exists(neighbourhoodsFile))
            {
                Console.WriteLine("Neighbours !");
                ReadParticleNeighbourhoods();
            }
            PlayResults(0);
			//ComputeNormals (0);

            string vectorFieldsFile = folder + "/VectorFields.mg";
            if (File.Exists(vectorFieldsFile))
            {
                ReadVectorFields();
            }
			//frame = 1;
        }

        // Update is called once per frame
        void Update()
        {
            int count = 0;
            if (play)
            {
                while (frame < nbOfFrames && count++ < speed)
                {
                    PlayResults(frame);
					//ComputeNormals (frame);
					frame++;
                }
                if (reset) {
                    PlayResults(0);
					//ComputeNormals (0);
                    reset = false;
                    play = false;
                    frame = 0;
                }       
            }
        }    

        Vector3[, ,] X;
        float[, ,] vPHI;
        Vector3[, ,] gradPHI;
        float[, ,] vPeronaMalik;
        Vector3[, ,] gradPeronaMalik;
        int resolution;
        void ReadVectorFields() {
            string vectorFieldsFile = folder + "/VectorFields.mg";

            string[] vectorFields = File.ReadAllLines(vectorFieldsFile);

            int resolution3 = vectorFields.Length-1;
            resolution = (int) Mathf.Pow(resolution3, 1f / 3);

            X = new Vector3[resolution,resolution,resolution];
            vPHI = new float[resolution, resolution, resolution];
            gradPHI = new Vector3[resolution, resolution, resolution];
            vPeronaMalik = new float[resolution, resolution, resolution];
            gradPeronaMalik = new Vector3[resolution, resolution, resolution];
            //Debug.Log(resolution3 + ", " + resolution);
            int line = 1;
            for (int i = 0; i < resolution3; i++) { 
                string[] strLine = vectorFields[line].Split(';');
                int I = int.Parse(strLine[0]);
                int J = int.Parse(strLine[1]);
                int K = int.Parse(strLine[2]);

                string[] strX = strLine[3].Split(',');
                X[I, J, K] = new Vector3(float.Parse(strX[0]), float.Parse(strX[1]), float.Parse(strX[2]));

                vPHI[I, J, K] = float.Parse(strLine[4]);

                string[] strGradPHI = strLine[5].Split(',');
                gradPHI[I, J, K] = new Vector3(float.Parse(strGradPHI[0]), float.Parse(strGradPHI[1]), float.Parse(strGradPHI[2]));

                vPeronaMalik[I, J, K] = float.Parse(strLine[6]);

                string[] strPeronaMalik = strLine[7].Split(',');
                gradPeronaMalik[I, J, K] = new Vector3(float.Parse(strPeronaMalik[0]), float.Parse(strPeronaMalik[1]), float.Parse(strPeronaMalik[2]));

                line++;
            }
        }

        void ReadResults()
        {
            string statesFile = folder + "/Results.mg";
            string numbersFile = folder + "/Numbers.mg";

            string[] results = File.ReadAllLines(statesFile);
            positions = new Vector3[results.Length - 1];

            string[] counts = File.ReadAllLines(numbersFile);
            numbers = new Vector3[counts.Length - 1];

            startOfFrame = new int[nbOfFrames];
            popSizePerFrame = new int[nbOfFrames];
            nbOfCellTypesPerFrame = new int[nbOfFrames];
            nbOfCellsPerTypePerFrame = new int[nbOfFrames][];
            nbOfVerticesPerCellPerFrame = new int[nbOfFrames][];
            nbOfVerticesPerTypePerCellPerFrame = new int[nbOfFrames][][];
            positionsPerTypePerCellPerVerticePerFrame = new Vector3[nbOfFrames][][][];


            int count = 0;
            int currentLineNumbers = 1;
            int frStart = 0;
            int populationSize = 0;
            for (int fr = 0; fr < nbOfFrames; fr++)
            {
                //if(fr>0) frStart += nbOfLinesPerFrameStates;
                nbOfLinesPerFrameStates = 0;
                for (int i = 0; i < populationSize; i++)
                {
                    nbOfLinesPerFrameStates += nbOfVerticesPerCell[i] + 1;
                }
                if (fr > 0)
                {
                    frStart += nbOfLinesPerFrameStates;
                    startOfFrame[fr] = frStart;
                }

                //currentLineNumbers--;
                //Debug.Log(counts[0]);
                //Debug.Log(counts.Length + ", " + currentLineNumbers);
                populationSize = int.Parse(counts[currentLineNumbers].Split(';')[2]);
                nbOfCellTypes = int.Parse(counts[currentLineNumbers].Split(';')[1]);
                nbOfCellsPerType = new int[nbOfCellTypes];
                nbOfVerticesPerTypePerCell = new int[nbOfCellTypes][];
                nbOfVerticesPerCell = new int[populationSize];
				//Debug.Log("population size: " + populationSize);

                popSizePerFrame[fr] = int.Parse(counts[currentLineNumbers].Split(';')[2]);
                nbOfCellTypesPerFrame[fr] = int.Parse(counts[currentLineNumbers].Split(';')[1]);
                nbOfCellsPerTypePerFrame[fr] = new int[nbOfCellTypesPerFrame[fr]];
                nbOfVerticesPerTypePerCellPerFrame[fr] = new int[nbOfCellTypesPerFrame[fr]][];
                nbOfVerticesPerCellPerFrame[fr] = new int[popSizePerFrame[fr]];
                positionsPerTypePerCellPerVerticePerFrame[fr] = new Vector3[nbOfCellTypesPerFrame[fr]][][];

                currentLineNumbers++;

                int cellTypeStartStates = 1;
                int cellTypeStartNumbers = 0;
                //for (int i = 0; i < nbOfCellTypes; i++)
                for (int i = 0; i < nbOfCellTypesPerFrame[fr]; i++)
                {
                    //Debug.Log("Hello");
                    //if (i > 0) cellTypeStartStates += nbOfCellsPerType[i - 1] * (nbOfVerticesPerType[i - 1] + 1);
                    if (i > 0)
                    {
                        for (int j = 0; j < nbOfCellsPerType[i - 1]; j++)
                        {
                            cellTypeStartStates += nbOfVerticesPerTypePerCell[i - 1][j] + 1;
                        }
                    }
                    if (i > 0) cellTypeStartNumbers += nbOfCellsPerType[i - 1];

                    nbOfCellsPerType[i] = int.Parse(counts[currentLineNumbers].Split(';')[2]);
                    nbOfVerticesPerTypePerCell[i] = new int[nbOfCellsPerType[i]];
                    currentLineNumbers++;

                    nbOfCellsPerTypePerFrame[fr][i] = nbOfCellsPerType[i];
                    nbOfVerticesPerTypePerCellPerFrame[fr][i] = nbOfVerticesPerTypePerCell[i];
                    positionsPerTypePerCellPerVerticePerFrame[fr][i] = new Vector3[nbOfCellsPerType[i]][];


                    //Debug.Log(fr + ", " + i + ", " + nbOfCellsPerTypePerFrame[fr][i]);
                    //for (int j = 0; j < nbOfCellsPerType[i]; j++)
                    for (int j = 0; j < nbOfCellsPerTypePerFrame[fr][i]; j++)
                    {
                        
                        nbOfVerticesPerTypePerCell[i][j] = int.Parse(counts[currentLineNumbers].Split(';')[2]);
						
						//Debug.Log(i + ", " + nbOfCellsPerType[i] + ", " + j + ", " + cellTypeStartNumbers);
                        nbOfVerticesPerCell[cellTypeStartNumbers + j] = nbOfVerticesPerTypePerCell[i][j];
                        currentLineNumbers++;

                        nbOfVerticesPerTypePerCellPerFrame[fr][i][j] = nbOfVerticesPerTypePerCell[i][j];
                        nbOfVerticesPerCellPerFrame[fr][cellTypeStartNumbers + j] = nbOfVerticesPerCell[cellTypeStartNumbers + j];
                        positionsPerTypePerCellPerVerticePerFrame[fr][i][j] = new Vector3[nbOfVerticesPerTypePerCell[i][j] + 1];

                        //int cellStart = frStart + cellTypeStartStates + j * (nbOfVerticesPerType[i]+1);
                        int cellStart = frStart + cellTypeStartStates;
                        if (j > 0)
                        {
                            cellStart += nbOfVerticesPerTypePerCell[i][j - 1] + 1;
                        }
                        cellStart = count + 1;

                        //Debug.Log(cellStart);
                        //for (int k = 0; k < nbOfVerticesPerType[i] + 1; k++)
                        for (int k = 0; k < nbOfVerticesPerTypePerCell[i][j] + 1; k++)
                        {
                            string vec = results[cellStart + k].Split(';')[3];
                            string[] xyz = vec
                                                //.Remove(vec.Length - 1)
                                                //.Remove(0, 1)
                                                .Split(',');

                            if (cellStart == 1)
                            {
                                //Debug.Log(vec);
                            }

                            positions[cellStart + k - 1] = new Vector3(float.Parse(xyz[0]),
                                                                    float.Parse(xyz[1]),
                                                                     float.Parse(xyz[2]));

                            positionsPerTypePerCellPerVerticePerFrame[fr][i][j][k] = positions[cellStart + k - 1];
                            count++;
                        }
                    }
                }
            }
        }

        void ReadParticleNeighbourhoods()
        {
            string neighbourhoodsFile = folder + "/InterCellEdges.mg";

            string[] particleNeighbourhoods = File.ReadAllLines(neighbourhoodsFile);

            //Debug.Log(nbOfFrames);
            
            externalEdges = new List<Vector4>[nbOfFrames];
            for(int i=0; i<nbOfFrames; i++)
            {
                externalEdges[i] = new List<Vector4>();
            }

            int currentLine = 1;
            int fr = -1, lastFrame = -1;
            while(currentLine < particleNeighbourhoods.Length)
            {
                //Debug.Log(particleNeighbourhoods[currentLine]);
                
                if (!String.IsNullOrEmpty(particleNeighbourhoods[currentLine]))
                {
                    string[] edge = particleNeighbourhoods[currentLine].Split(';');

                    int readFrame = int.Parse(edge[0]);
                    if (readFrame != lastFrame)
                    {
                        fr++;
                    }
                    //Debug.Log(nbOfFrames + ", " + readFrame);

                    externalEdges[fr].Add(new Vector4(
                    //externalEdges[readFrame-1].Add(new Vector4(
                            float.Parse(edge[1]),
                            float.Parse(edge[2]),
                            float.Parse(edge[3]),
                            float.Parse(edge[4])
                    ));

                    lastFrame = readFrame;
                }
    
                currentLine++;
            }
        }

        int lineNumber = 0;
        int cellLineNumber = 0;
        void PlayResults(int frame)
        {
            int frameStartStates = nbOfLinesPerFrameStates * frame;
            int cStart = 0;
            
            Vector3[] vs;
            for (int i = 0; i < nbOfCellTypesPerFrame[frame]; i++)
            {
                int popGrowth = 0;
                if (frame > 0 && nbOfCellsPerTypePerFrame[frame][i] > nbOfCellsPerTypePerFrame[frame - 1][i])
                {
					Debug.Log ("Division");
                    popGrowth = nbOfCellsPerTypePerFrame[frame][i] - nbOfCellsPerTypePerFrame[frame - 1][i];
                    int l = 0;
                    while (l++ < popGrowth && popSize<maxPopSize)
                    {
                        AddCell(i);
                    }
                }


                //if (i > 0) cStart += nbOfCellsPerType[i - 1];
				if (i > 0) cStart += nbOfCellsPerTypePerFrame[frame][i-1];

				//Debug.Log(nbOfCellsPerTypePerFrame[frame][i]);
                for (int j = 0; j < nbOfCellsPerTypePerFrame[frame][i]; j++)
                {
                    vs = new Vector3[nbOfVerticesPerTypePerCellPerFrame[frame][i][j]];
                    cellLineNumber++;
                    
					//Debug.Log(popSize + ", " + i + ", " + cStart + ", " + j);
					//Debug.Log(cells[cStart + j].color);
					
                    for (int k = 0; k < nbOfVerticesPerTypePerCellPerFrame[frame][i][j]; k++)
                    {
                        vs[k] = positionsPerTypePerCellPerVerticePerFrame[frame][i][j][k];

                        //cells[cStart + j].particles[k].transform.localPosition = vs[k];
                        tissues[i].cells[j].particles[k].transform.localPosition = vs[k];
						lineNumber++;
                    }
                    //Debug.Log(nbOfVerticesPerTypePerCellPerFrame[frame][i][j]);
                    //cells[cStart + j].GetComponent<MeshFilter>().mesh.vertices = vs;
                    //cells[cStart + j].nuclei[0] = positionsPerTypePerCellPerVerticePerFrame[frame][i][j][nbOfVerticesPerTypePerCellPerFrame[frame][i][j]];

                    //cells[cStart + j].GetComponent<MeshFilter>().mesh.RecalculateNormals();
                    //cells[cStart + j].GetComponent<MeshFilter>().mesh.RecalculateTangents();

                    tissues[i].cells[j].GetComponent<MeshFilter>().mesh.vertices = vs;
                    tissues[i].cells[j].nuclei[0] = positionsPerTypePerCellPerVerticePerFrame[frame][i][j][nbOfVerticesPerTypePerCellPerFrame[frame][i][j]];

                    tissues[i].cells[j].GetComponent<MeshFilter>().mesh.RecalculateNormals();
                    tissues[i].cells[j].GetComponent<MeshFilter>().mesh.RecalculateTangents();
                }
            }
            //*/

        }

		Vector3[] triNormals;
		public void ComputeNormals(int frame){
			Mesh m;
			int cStart = 0;
			for (int i = 0; i < nbOfCellTypesPerFrame [frame]; i++) {
				
				if (i > 0) cStart += nbOfCellsPerTypePerFrame[frame][i-1];
				for (int j = 0; j < nbOfCellsPerTypePerFrame [frame] [i]; j++) {

					m = cells [cStart + j].GetComponent<MeshFilter> ().mesh;
					triNormals = new Vector3[m.triangles.Length/3];
					//Debug.Log (frame + ": " + m.triangles.Length);

					//Debug.Log (cStart + j);
					for (int k = 0; k < m.vertexCount; k++) {
						
						//Debug.Log (frame + ": " + m.normals.Length);
						m.normals[k] = Vector3.zero;
					}

					for (int k = 0; k < m.triangles.Length; k += 3) {
						
						triNormals [k / 3] = Vector3.Cross (m.vertices [m.triangles[k + 1]] - m.vertices [m.triangles[k]], 
															m.vertices [m.triangles[k + 2]] - m.vertices [m.triangles[k]]);

						m.normals [m.triangles [k]] += triNormals [k / 3];
						m.normals [m.triangles [k + 1]] += triNormals [k / 3];
						m.normals [m.triangles [k + 2]] += triNormals [k / 3];
					}

					for (int k = 0; k < m.vertexCount; k++) {
						m.normals[k].Normalize();
					}
				}
			}
		}

        void ReadParameters()
        {
            string parFile = folder + "/Parameters.mg";
            
            string[] parameters = File.ReadAllLines(parFile);

            int currentLine = 1;
            int.TryParse(parameters[currentLine++].Split('=')[1], out nbOfFrames);

            while (parameters[currentLine++] != string.Empty) { }
            currentLine++;

            int.TryParse(parameters[currentLine++].Split('=')[1], out popSize);
            int.TryParse(parameters[currentLine++].Split('=')[1], out maxPopSize);
            int.TryParse(parameters[currentLine++].Split('=')[1], out nbOfCellTypes);

            nbOfCellsPerType = new int[nbOfCellTypes];
            for (int i = 0; i < nbOfCellTypes; i++)
            {
                int.TryParse(parameters[currentLine++].Split('=')[1], out nbOfCellsPerType[i]);
            }

            while (parameters[currentLine++] != string.Empty) { }

            currentLine += nbOfCellTypes*(nbOfCellTypes + 1)/2 + nbOfCellTypes + 4;
            
            nbOfVerticesPerType = new int[nbOfCellTypes];
            nbOfTrianglesPerType = new int[nbOfCellTypes];
            meshPerCellType = new Mesh[nbOfCellTypes];

            int step = 0, pos = currentLine;
            Vector3[] vs;
            int[] tris;
            for (int i = 0; i < nbOfCellTypes; i++)
            {
                int.TryParse(parameters[step + pos].Split('=')[1], out nbOfVerticesPerType[i]);
                int.TryParse(parameters[step + pos + 1].Split('=')[1], out nbOfTrianglesPerType[i]);

                currentLine += 4;
                vs = new Vector3[nbOfVerticesPerType[i]];
                for (int j = 0; j < nbOfVerticesPerType[i]; j++)
                {
                    string[] xyz = parameters[currentLine++]
                        //.Remove(parameters[currentLine - 1].Length - 1)
                        //.Remove(0, 1)
                        .Split(',');

                    //Debug.Log(j);
                    vs[j] = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                    //Debug.Log(vs[j]);
                }
                currentLine += 2;
                tris = new int[3 * nbOfTrianglesPerType[i]];
                for (int j = 0; j < nbOfTrianglesPerType[i]; j++)
                {
                    string[] xyz = parameters[currentLine++].Split(',');
                    tris[3 * j] = int.Parse(xyz[0]);
                    tris[3 * j + 1] = int.Parse(xyz[1]);
                    tris[3 * j + 2] = int.Parse(xyz[2]);
                }
                currentLine += 3;
                step += nbOfVerticesPerType[i] + nbOfTrianglesPerType[i] + 9;

                meshPerCellType[i] = new Mesh();
                meshPerCellType[i].vertices = vs;
                meshPerCellType[i].triangles = tris;
            }
        }

        void CreateCellPopulation()
        {
            cells = new MGCell[maxPopSize];
            tissues = new MGTissue[nbOfCellTypes];
            
            int cellTypeStartStates = 0;
            for (int i = 0; i < nbOfCellTypes; i++)
            {
                tissues[i] = Instantiate(tissuePrefab);
                tissues[i].cells = new MGCell[maxPopSize];
                tissues[i].transform.SetParent(transform);
                tissues[i].name += "" + i;
                tissues[i].popSize = nbOfCellsPerType[i];

                if (i > 0) cellTypeStartStates += nbOfCellsPerType[i - 1];
                for (int j = 0; j < nbOfCellsPerType[i]; j++)
                {
                    cells[cellTypeStartStates + j] = Instantiate(cellPrefab);
                    cells[cellTypeStartStates + j].nuclei = new Vector3[2];
                    cells[cellTypeStartStates + j].GetComponent<MeshFilter>().mesh = meshPerCellType[i];
					cells [cellTypeStartStates + j].GetComponent<MeshRenderer> ().material.color = tissueColors [i];
                    
					cells[cellTypeStartStates + j].name += "" + (cellTypeStartStates + j);
                    cells[cellTypeStartStates + j].transform.SetParent(tissues[i].transform);

                    tissues[i].cells[j] = cells[cellTypeStartStates + j];

					cells[cellTypeStartStates + j].particles = new MGParticle[nbOfVerticesPerType[i]];
                    for (int k = 0; k < nbOfVerticesPerType[i]; k++)
                    {
                        cells[cellTypeStartStates + j].particles[k] = Instantiate(particlePrefab);
                        cells[cellTypeStartStates + j].particles[k].name = "MGParticle" + "_" + j + "_" + k;
                        //cells[cellTypeStartStates + j].particles[k].externalNeighbours = new List<MGParticle>();
                        cells[cellTypeStartStates + j].particles[k].transform.SetParent(cells[cellTypeStartStates + j].transform);
                    }
                }
            }
        }

        void AddCell(int tissueId)
        {
            cells[popSize] = Instantiate(cellPrefab);
            cells[popSize].nuclei = new Vector3[2];
            cells[popSize].GetComponent<MeshFilter>().mesh = meshPerCellType[tissueId];
			cells[popSize].transform.SetParent(tissues[tissueId].transform);
			cells [popSize].GetComponent<MeshRenderer> ().material.color = tissueColors [tissueId];
			cells [popSize].GetComponent<MeshFilter> ().mesh.RecalculateNormals();
			cells [popSize].GetComponent<MeshFilter> ().mesh.RecalculateTangents();

			cells[popSize].particles = new MGParticle[meshPerCellType[tissueId].vertexCount];
            for (int k = 0; k < meshPerCellType[tissueId].vertexCount; k++)
            {
                cells[popSize].particles[k] = Instantiate(particlePrefab);
                cells[popSize].particles[k].name = "MGParticle" + "_" + tissues[tissueId].popSize + "_" + k;
                cells[popSize].particles[k].transform.SetParent(cells[popSize].transform);
            }

			tissues[tissueId].cells[tissues[tissueId].popSize] = cells[popSize];
			tissues [tissueId].popSize++;

            popSize++;
        }

        void OnApplicationQuit()
        {
            for(int i=0; i<cells.Length; i++)
            {
                Destroy(cells[i]);
            }
            positions = new Vector3[0];
            numbers = new Vector3[0];
            states = new List<PersistantVertex>(0);
        }

        public void OnDrawGizmos()
        {
            if (Application.isPlaying) { 
                for(int i=0; i<popSize; i++)
                {
                    //DrawCell(particleDrawn);
                    DrawCell(i);
                }
                DrawExternalEdges();
                DrawVectorFields();
            }
        }

        //*
        public void DrawCell(int id) {
            //Debug.Log(id);
            Mesh mesh = cells[id].GetComponent<MeshFilter>().mesh;

            if (nucleus)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(cells[id].nuclei[0], 0.1f);
            }

            for (int i = 0; i < mesh.vertexCount; i++)
            {  
                if (particles)
                {
                    Gizmos.color = Color.white;
                    if (id > 0)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawSphere(mesh.vertices[i], 1.0f);
                    }    
                }
                
                /*
                if (adhesionRays)
                {
                    Gizmos.color = Color.yellow;
                    
                    for (int j = 0; j < cells[id].particles[i].externalNeighbours.Count; j++)
                    {
                        Gizmos.DrawLine(mesh.vertices[i], cells[id].particles[i].externalNeighbours[j].transform.localPosition);
                    }
                }
                */

                if (radialRays)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(mesh.vertices[i], cells[id].nuclei[0]);
                }    
            }

            if (membraneRays)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < cells[id].GetComponent<MeshFilter>().mesh.triangles.Length; i += 3)
                {
                    Gizmos.DrawLine(mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]]);
                    Gizmos.DrawLine(mesh.vertices[mesh.triangles[i +1]], mesh.vertices[mesh.triangles[i + 2]]);
                    Gizmos.DrawLine(mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 2]]);
                }
            }
        }
        //*/

        public void DrawExternalEdges()
        {
            if (adhesionRays)
            {
                Gizmos.color = Color.yellow;
                //Debug.Log(externalEdges[frame].Count);
                //*
                //if (externalEdges[frame].IsNull()) ;

                if (frame < nbOfFrames) { 
                    for (int i = 0; i < externalEdges[frame].Count; i++)
                    {
                        //Debug.Log(externalEdges[frame][i]);

                        if (externalEdges[frame][i].x < popSize && externalEdges[frame][i].z < popSize)
                        {
                            Gizmos.DrawLine(cells[(int)externalEdges[frame][i].x].particles[(int)externalEdges[frame][i].y].transform.position,
                            cells[(int)externalEdges[frame][i].z].particles[(int)externalEdges[frame][i].w].transform.position);
                        }
                    }
                }
                //*/
            }    
        }

        public void DrawVectorFields()
        {
            if (drawDiffusion)
            {
                Gizmos.color = Color.blue;// * vPHI[i,j,k];
                Gizmos.DrawSphere(X[0, 0, 0], 0.05f);
                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        for (int k = 0; k < resolution; k++)
                        {
                            if ((i + j + k) != 0)
                            {
                                Gizmos.color = Color.white * vPHI[i,j,k];
                                Gizmos.DrawSphere(X[i, j, k], 0.5f);
                            }
                        }
                    }
                }
            }

            if (drawVectorField)
            {
                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        for (int k = 0; k < resolution; k++)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(X[i, j, k], X[i, j, k] - gradPeronaMalik[i, j, k]);
                        }
                    }
                }
            }
        }
    }
}