using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VeilHouse
{
    /// <summary>Small original mesh authoring utility. All coordinates are in avatar bind space.</summary>
    internal sealed class DetectiveMesh
    {
        internal struct Section
        {
            public float y, width, depth; public int bone;
            public Section(float y,float width,float depth,int bone) {this.y=y;this.width=width;this.depth=depth;this.bone=bone;}
        }
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<BoneWeight> weights=new List<BoneWeight>();
        readonly List<int>[] triangles;
        internal DetectiveMesh(int materials)
        {
            triangles=new List<int>[materials];
            for(int i=0;i<materials;i++) triangles[i]=new List<int>();
        }
        int Add(Vector3 position,int bone,Vector2 tex)
        {
            int index=vertices.Count;vertices.Add(position);uv.Add(tex);
            weights.Add(new BoneWeight{boneIndex0=bone,weight0=1});
            return index;
        }
        void Tri(int a,int b,int c,int mat) {triangles[mat].Add(a);triangles[mat].Add(b);triangles[mat].Add(c);}
        internal void Vertical(Section[] sections,Vector3 offset,int sides,int material)
        {
            int first=vertices.Count;
            for(int r=0;r<sections.Length;r++) {
                Section section=sections[r];
                for(int i=0;i<=sides;i++) {
                    float angle=i*Mathf.PI*2/sides;
                    Add(offset+new Vector3(Mathf.Sin(angle)*section.width,section.y,Mathf.Cos(angle)*section.depth),section.bone,new Vector2((float)i/sides,(float)r/(sections.Length-1)));
                }
            }
            JoinRings(first,sections.Length,sides,material);
            CapVertical(sections[0],offset,sides,material,false);
            CapVertical(sections[sections.Length-1],offset,sides,material,true);
        }
        void CapVertical(Section section,Vector3 offset,int sides,int material,bool top)
        {
            int center=Add(offset+new Vector3(0,section.y,0),section.bone,new Vector2(.5f,.5f));
            int first=vertices.Count;
            for(int i=0;i<sides;i++) {
                float angle=i*Mathf.PI*2/sides;
                Add(offset+new Vector3(Mathf.Sin(angle)*section.width,section.y,Mathf.Cos(angle)*section.depth),section.bone,new Vector2(Mathf.Sin(angle)*.5f+.5f,Mathf.Cos(angle)*.5f+.5f));
            }
            for(int i=0;i<sides;i++) {int a=first+i,b=first+(i+1)%sides;if(top) Tri(center,a,b,material);else Tri(center,b,a,material);}
        }
        void JoinRings(int first,int count,int sides,int material)
        {
            for(int r=0;r<count-1;r++) for(int i=0;i<sides;i++) {
                int a=first+r*(sides+1)+i,b=a+1,c=b+sides+1,d=a+sides+1;
                Tri(a,b,c,material);Tri(a,c,d,material);
            }
        }
        internal void Tube(Vector3[] points,float[] radii,int[] boneIds,int sides,int material)
        {
            int first=vertices.Count;
            Vector3 direction=(points[points.Length-1]-points[0]).normalized;
            Vector3 up=Mathf.Abs(direction.y)<.95f?Vector3.up:Vector3.forward;
            Vector3 other=Vector3.Cross(direction,up).normalized;
            up=Vector3.Cross(other,direction).normalized;
            for(int r=0;r<points.Length;r++) for(int i=0;i<=sides;i++) {
                float a=i*Mathf.PI*2/sides;
                Add(points[r]+(up*Mathf.Cos(a)+other*Mathf.Sin(a))*radii[r],boneIds[r],new Vector2((float)i/sides,(float)r/(points.Length-1)));
            }
            JoinRings(first,points.Length,sides,material);
            for(int end=0;end<2;end++) {
                int r=end==0?0:points.Length-1;
                int center=Add(points[r],boneIds[r],new Vector2(.5f,.5f));
                for(int i=0;i<sides;i++) {
                    int a=first+r*(sides+1)+i,b=a+1;
                    if(end==1)Tri(center,a,b,material);else Tri(center,b,a,material);
                }
            }
        }
        internal void Ellipsoid(Vector3 center,Vector3 radius,int bone,int material,int sides=10,int rows=6)
        {
            int first=vertices.Count;
            for(int r=0;r<=rows;r++) {
                float phi=Mathf.Lerp(-Mathf.PI*.5f,Mathf.PI*.5f,(float)r/rows);
                for(int i=0;i<=sides;i++) {
                    float a=i*Mathf.PI*2/sides;
                    Vector3 p=new Vector3(Mathf.Sin(a)*Mathf.Cos(phi)*radius.x,Mathf.Sin(phi)*radius.y,Mathf.Cos(a)*Mathf.Cos(phi)*radius.z);
                    Add(center+p,bone,new Vector2((float)i/sides,(float)r/rows));
                }
            }
            JoinRings(first,rows+1,sides,material);
        }
        internal void Polygon(Vector3[] points,int bone,int material)
        {
            // Separate back vertices preserve normals on thin clothing details.
            for(int face=0;face<2;face++) {
                int first=vertices.Count;
                for(int i=0;i<points.Length;i++) Add(points[i],bone,new Vector2(points[i].x,points[i].y));
                for(int i=1;i<points.Length-1;i++) if(face==0)Tri(first,first+i,first+i+1,material);else Tri(first,first+i+1,first+i,material);
            }
        }
        internal void Box(Vector3 center,Vector3 size,int bone,int material)
        {
            Vector3 h=size*.5f;
            Vector3[] p={center+new Vector3(-h.x,-h.y,-h.z),center+new Vector3(h.x,-h.y,-h.z),center+new Vector3(h.x,h.y,-h.z),center+new Vector3(-h.x,h.y,-h.z),center+new Vector3(-h.x,-h.y,h.z),center+new Vector3(h.x,-h.y,h.z),center+new Vector3(h.x,h.y,h.z),center+new Vector3(-h.x,h.y,h.z)};
            int[,] faces={{0,3,2,1},{4,5,6,7},{0,4,7,3},{1,2,6,5},{0,1,5,4},{3,7,6,2}};
            for(int f=0;f<6;f++) {
                int first=vertices.Count;
                for(int i=0;i<4;i++)Add(p[faces[f,i]],bone,new Vector2(i==1||i==2?1:0,i>=2?1:0));
                Tri(first,first+1,first+2,material);Tri(first,first+2,first+3,material);
            }
        }
        internal Mesh Create(List<Transform> bones,Transform root)
        {
            var mesh=new Mesh{name="Original tailored detective / skinned mesh",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();
            mesh.subMeshCount=triangles.Length;
            for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i);
            var bind=new Matrix4x4[bones.Count];
            for(int i=0;i<bones.Count;i++)bind[i]=bones[i].worldToLocalMatrix*root.localToWorldMatrix;
            mesh.bindposes=bind;mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }
    }
}
