using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using UnityEngine;

public class Chunk
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public GameObject GameObj;
    private BlockLibrary _blockLibrary;
    
    
    private MeshBuilder blockMeshBuilder;
    private MeshBuilder waterMeshBuilder;
    
    
    Dictionary<Globals.Direction, Chunk> chunkNeighbors = new Dictionary<Globals.Direction, Chunk>();
    
    private ChunkData _chunkData;
    public ChunkData Data => _chunkData;
    public Chunk(ChunkData data, GameObject obj, BlockLibrary blockLibrary)
    {
        _chunkData = data;
        GameObj = obj;
        _blockLibrary = blockLibrary;

        blockMeshBuilder = new MeshBuilder();
        waterMeshBuilder = new MeshBuilder();
        
        meshFilter = GameObj.GetComponent<MeshFilter>();
        meshCollider = GameObj.GetComponent<MeshCollider>();
    }

    public void GenerateMeshData()
    {
        Profiler.BeginSample("Generate Mesh Data");
        for (int x = 0; x < Globals.ChunkSize; x++) 
        {
            for (int z = 0; z < Globals.ChunkSize; z++)
            {
                for (int y = 0; y < Globals.ChunkHeight; y++)
                {
                    byte block = _chunkData.GetBlock(x, y, z);
                    if (block != (byte) BlockType.AIR)
                    {
                        AddBlock(x, y, z, block);
                    }
                }
            }
        }
        Profiler.EndSample();
    }
    
    public void Load()
    {
        CreateMesh();
    }

    public void UnLoad()
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh = null;
        meshCollider.sharedMesh = null;
    }
    void AddBlock(int x, int y, int z, byte block)
    {
        foreach (var dirKvp in Globals.Directions_3D)
        {
            var dir = dirKvp.Value;
            int nx = x + dir.x;
            int ny = y + dir.y;
            int nz = z + dir.z;
            if (TryGetBlock(nx, ny, nz, out byte targetBlock))
            {
                if (block == (byte) BlockType.WATER)
                {
                    if (targetBlock != (byte) BlockType.AIR) continue;
                    waterMeshBuilder.AddFace(_blockLibrary[block].MeshData.GetFaceData(dirKvp.Key), dirKvp.Key, new Vector3Int(x, y, z), _blockLibrary[block].GetTextureSliceIndex(dirKvp.Key), _blockLibrary[block].DefaultColor);
                }
                else
                {
                    if(targetBlock is (byte) BlockType.AIR or (byte) BlockType.WATER || _blockLibrary[targetBlock].IsTransparent)
                    {
                        blockMeshBuilder.AddFace(_blockLibrary[block].MeshData.GetFaceData(dirKvp.Key),dirKvp.Key, new Vector3Int(x, y, z), _blockLibrary[block].GetTextureSliceIndex(dirKvp.Key), _blockLibrary[block].DefaultColor);
                    }
                }
            }
        }
    }

    void CreateMesh()
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        combineInstances.Add(new CombineInstance()
        {
            mesh = blockMeshBuilder.Build(),
            subMeshIndex = 0
        });
        combineInstances.Add(new CombineInstance()
        {
            mesh = waterMeshBuilder.Build(),
            subMeshIndex = 0
        });
        Mesh finalMesh = new Mesh
        {
            subMeshCount = 2
        };
        finalMesh.CombineMeshes(combineInstances.ToArray(), false, false);
        
        meshFilter.mesh = finalMesh;
        meshCollider.sharedMesh = finalMesh;
    }
    public void Clear()
    {
        meshFilter.mesh.Clear();
        blockMeshBuilder.Clear();
        waterMeshBuilder.Clear();
    }

    public void SetNeighbor(Globals.Direction dir, Chunk c)
    {
        chunkNeighbors[dir] = c;
    }

    public bool TryGetBlock(int x, int y, int z, out byte block)
    {
        block = (byte)BlockType.AIR;

        if (_chunkData.IsWithinChunk(x, y, z))
        {
            block = _chunkData.GetBlock(x, y, z);
            return true;
        }
        else
        {
            //Get from neighbors
            // Handle corner cases
            if (x < 0 && z < 0 && 
                chunkNeighbors.ContainsKey(Globals.Direction.LEFT) && 
                chunkNeighbors[Globals.Direction.LEFT].chunkNeighbors.ContainsKey(Globals.Direction.BACK))
            {
                block = chunkNeighbors[Globals.Direction.LEFT].chunkNeighbors[Globals.Direction.BACK].Data.GetBlock(
                    Globals.ChunkSize - 1, y, Globals.ChunkSize - 1);
                return true;
            }
            if (x < 0 && chunkNeighbors.ContainsKey(Globals.Direction.LEFT))
            {
                block = chunkNeighbors[Globals.Direction.LEFT].Data.GetBlock(Globals.ChunkSize - 1, y, z);
                return true;
            }
            if (x > Globals.ChunkSize - 1 && chunkNeighbors.ContainsKey(Globals.Direction.RIGHT))
            {
                block = chunkNeighbors[Globals.Direction.RIGHT].Data.GetBlock(0, y, z);
                return true;
            }

            if (z < 0 && chunkNeighbors.ContainsKey(Globals.Direction.BACK))
            {
                block = chunkNeighbors[Globals.Direction.BACK].Data.GetBlock(x, y, Globals.ChunkSize - 1);
                return true;
            }
            if (z > Globals.ChunkSize - 1 && chunkNeighbors.ContainsKey(Globals.Direction.FRONT))
            {
                block = chunkNeighbors[Globals.Direction.FRONT].Data.GetBlock(x, y, 0);
                return true;
            }
        }
        
        return false;
    }
    public void UpdateNeighbors(int x, int z)
    {
        if (x == 0 && chunkNeighbors.TryGetValue(Globals.Direction.LEFT, out var leftChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(leftChunk.Data.ChunkPosition.x ,0 ,leftChunk.Data.ChunkPosition.y)) return;
            leftChunk.Clear();
            leftChunk.GenerateMeshData();
            leftChunk.Load();
        }
        else if (x == Globals.ChunkSize - 1 && chunkNeighbors.TryGetValue(Globals.Direction.RIGHT, out var rightChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(rightChunk.Data.ChunkPosition.x ,0 ,rightChunk.Data.ChunkPosition.y)) return;
            rightChunk.Clear();
            rightChunk.GenerateMeshData();
            rightChunk.Load();
        }
    
        if (z == 0 && chunkNeighbors.TryGetValue(Globals.Direction.BACK, out var backChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(backChunk.Data.ChunkPosition.x ,0 ,backChunk.Data.ChunkPosition.y)) return;
            backChunk.Clear();
            backChunk.GenerateMeshData();
            backChunk.Load();
        }
        else if (z == Globals.ChunkSize - 1 && chunkNeighbors.TryGetValue(Globals.Direction.FRONT, out var frontChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(frontChunk.Data.ChunkPosition.x ,0 ,frontChunk.Data.ChunkPosition.y)) return;
            frontChunk.Clear();
            frontChunk.GenerateMeshData();
            frontChunk.Load();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(GameObj.transform.position.x + Globals.ChunkSize / 2, Globals.ChunkSize, GameObj.transform.position.z + Globals.ChunkSize / 2), new Vector3(1, Globals.ChunkHeight/Globals.ChunkSize, 1) * Globals.ChunkSize);
    }
}
