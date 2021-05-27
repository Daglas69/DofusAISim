using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupCircle : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float radius;
    private Color color;

    // Start is called before the first frame update
    void Start()
    {
        //Size
        var gridManager = GameObject.FindObjectOfType<GridManager>();
        radius = (gridManager.grid[1,1].world_pos.y - gridManager.grid[0,0].world_pos.y)/2.5f;
        
        //Color
        int grp = gameObject.GetComponent<Character>().grp;
        color = grp == 1 ? Color.blue : Color.red;

        //Render
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.07f;
        lineRenderer.endWidth = 0.07f;
        lineRenderer.loop = true;
    }

    // Update is called once per frame
    void Update()
    {
        //Draw group circle
        DrawPolygon(gameObject.transform.position);
    }

    void DrawPolygon(Vector3 centerPos, int vertexNumber = 300)
    {
        float angle = 2 * Mathf.PI / vertexNumber;
        lineRenderer.positionCount = vertexNumber;
        for (int i = 0; i < vertexNumber; i++)
        {
            Matrix4x4 rotationMatrix = new Matrix4x4(
                new Vector4(Mathf.Cos(angle * i), Mathf.Sin(angle * i), 0, 0),
                new Vector4(-1 * Mathf.Sin(angle * i), Mathf.Cos(angle * i), 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );
            Vector3 initialRelativePosition = new Vector3(0, radius, 0);
            lineRenderer.SetPosition(i, centerPos + rotationMatrix.MultiplyPoint(initialRelativePosition));
        }
    }
}
