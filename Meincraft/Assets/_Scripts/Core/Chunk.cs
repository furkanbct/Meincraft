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
    private BlockTextureLibrary _blockTextures;
    
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Vector3> _normals;
    private List<Color> _colors;
    private List<Vector3> _uvs;

    private readonly Dictionary<Vector3Int, Action<int,int,int, byte>> _directionActions;
    
    private ChunkData _chunkData;
    public ChunkData Data => _chunkData;
    public Chunk(ChunkData data, GameObject obj, BlockTextureLibrary blockTextures)
    {
        _chunkData = data;
        GameObj = obj;
        _blockTextures = blockTextures;
        
        meshFilter = GameObj.GetComponent<MeshFilter>();
        meshCollider = GameObj.GetComponent<MeshCollider>();

        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _normals = new List<Vector3>();
        _colors = new List<Color>();
        _uvs = new List<Vector3>();
        
        _directionActions = new Dictionary<Vector3Int, Action<int,int,int, byte>>
        {
            { Globals.Directions_3D[(byte)Globals.Direction.FRONT], FrontFace },
            { Globals.Directions_3D[(byte)Globals.Direction.UP], TopFace },
            { Globals.Directions_3D[(byte)Globals.Direction.RIGHT], RightFace },
            { Globals.Directions_3D[(byte)Globals.Direction.BACK], BackFace },
            { Globals.Directions_3D[(byte)Globals.Direction.DOWN], BottomFace },
            { Globals.Directions_3D[(byte)Globals.Direction.LEFT], LeftFace }
        };
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
                    if(block != (byte)BlockType.AIR)
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
        foreach (var dir in Globals.Directions_3D)
        {
            Vector3Int pos = new Vector3Int(x + dir.x, y + dir.y, z + dir.z);
            if (_chunkData.IsWithinChunk(pos))
            {
                if(_chunkData.GetBlock(pos) == (byte)BlockType.AIR) _directionActions[dir].Invoke(x, y, z, block);
            }
            else
            {
                pos += _chunkData.ChunkPosition.ToVector3Int();//Convert to global position
                if (World.Instance.TryGetBlock(pos.x, pos.y, pos.z, out byte targetBlock))
                {
                    if (targetBlock == (byte)BlockType.AIR)
                    {
                        _directionActions[dir].Invoke(x, y, z, block);
                    }
                }
            }
        }
    }

    void AddFace()
    {
        int vertCount = _vertices.Count;

        // First triangle
        _triangles.Add(vertCount - 4);
        _triangles.Add(vertCount - 3);
        _triangles.Add(vertCount - 2);

        // Second triangle
        _triangles.Add(vertCount - 4);
        _triangles.Add(vertCount - 2);
        _triangles.Add(vertCount - 1);
    }
    void BackFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x + 1, y, z));
        _vertices.Add(new Vector3(x, y, z));
        _vertices.Add(new Vector3(x, y + 1, z));
        _vertices.Add(new Vector3(x + 1, y + 1, z));

        _normals.Add(Vector3.back);
        _normals.Add(Vector3.back);
        _normals.Add(Vector3.back);
        _normals.Add(Vector3.back);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].BackFace);
    }
    void FrontFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x, y, z + 1));
        _vertices.Add(new Vector3(x + 1, y, z + 1));
        _vertices.Add(new Vector3(x + 1, y + 1, z + 1));
        _vertices.Add(new Vector3(x, y + 1, z + 1));
        
        _normals.Add(Vector3.forward);
        _normals.Add(Vector3.forward);
        _normals.Add(Vector3.forward);
        _normals.Add(Vector3.forward);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].FrontFace);
    }
    void TopFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x, y + 1, z));
        _vertices.Add(new Vector3(x, y + 1, z + 1));
        _vertices.Add(new Vector3(x + 1, y + 1, z + 1));
        _vertices.Add(new Vector3(x + 1, y + 1, z));

        _normals.Add(Vector3.up);
        _normals.Add(Vector3.up);
        _normals.Add(Vector3.up);
        _normals.Add(Vector3.up);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].TopFace);
    }
    void BottomFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x, y, z));
        _vertices.Add(new Vector3(x + 1, y, z));
        _vertices.Add(new Vector3(x + 1, y, z + 1));
        _vertices.Add(new Vector3(x, y, z + 1));

        _normals.Add(Vector3.down);
        _normals.Add(Vector3.down);
        _normals.Add(Vector3.down);
        _normals.Add(Vector3.down);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].BottomFace);
    }
    void LeftFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x, y, z));
        _vertices.Add(new Vector3(x, y, z + 1));
        _vertices.Add(new Vector3(x, y + 1, z + 1));
        _vertices.Add(new Vector3(x, y + 1, z));

        _normals.Add(Vector3.left);
        _normals.Add(Vector3.left);
        _normals.Add(Vector3.left);
        _normals.Add(Vector3.left);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].LeftFace);
    }
    void RightFace(int x, int y, int z, byte block)
    {
        _vertices.Add(new Vector3(x + 1, y, z + 1));
        _vertices.Add(new Vector3(x + 1, y, z));
        _vertices.Add(new Vector3(x + 1, y + 1, z));
        _vertices.Add(new Vector3(x + 1, y + 1, z + 1));

        _normals.Add(Vector3.right);
        _normals.Add(Vector3.right);
        _normals.Add(Vector3.right);
        _normals.Add(Vector3.right);
        
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        _colors.Add(Color.white);
        
        AddFace();
        AddUV(_blockTextures.Data[block].RightFace);
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        
        mesh.SetVertices(_vertices);
        mesh.SetTriangles(_triangles, 0);
        mesh.SetUVs(0, _uvs);
        mesh.SetNormals(_normals);
        mesh.SetColors(_colors);
        mesh.name = _chunkData.ChunkPosition.ToString();
        
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    void AddUV(int textureSliceIndex)
    {
        _uvs.AddRange(new Vector3[]
        {
            new Vector3(0.0f, 0.0f, textureSliceIndex),
            new Vector3(1.0f, 0.0f, textureSliceIndex),
            new Vector3(1.0f, 1.0f, textureSliceIndex),
            new Vector3(0.0f, 1.0f, textureSliceIndex)
        });
    }
    public void Clear()
    {
        _vertices.Clear();
        _triangles.Clear();
        _normals.Clear();
        _colors.Clear();
        _uvs.Clear();
    }
    public void UpdateNeighbors(int x, int z)
    {
        Vector2Int globalPos = new Vector2Int(_chunkData.ChunkPosition.x + x, _chunkData.ChunkPosition.y + z);
        if (x == 0)
        {
            if (World.Instance.GetChunkAtCoord(globalPos.x - 1, globalPos.y,  out var chunk))
            {
                chunk.Clear();
                chunk.GenerateMeshData();
                chunk.Load();
            }
        }
        else if (x == Globals.ChunkSize - 1)
        {
            if (World.Instance.GetChunkAtCoord(globalPos.x + 1, globalPos.y,  out var chunk))
            {
                chunk.Clear();
                chunk.GenerateMeshData();
                chunk.Load();
            }
        }
        
        if (z == 0)
        {
            if (World.Instance.GetChunkAtCoord(globalPos.x , globalPos.y - 1,  out var chunk))
            {
                chunk.Clear();
                chunk.GenerateMeshData();
                chunk.Load();
            }
        }
        else if (z == Globals.ChunkSize - 1)
        {
            if (World.Instance.GetChunkAtCoord(globalPos.x , globalPos.y + 1,  out var chunk))
            {
                chunk.Clear();
                chunk.GenerateMeshData();
                chunk.Load();
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(GameObj.transform.position.x + Globals.ChunkSize / 2, Globals.ChunkSize, GameObj.transform.position.z + Globals.ChunkSize / 2), new Vector3(1, Globals.ChunkHeight/Globals.ChunkSize, 1) * Globals.ChunkSize);
    }
}
