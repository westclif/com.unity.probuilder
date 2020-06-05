﻿using System.Collections.Generic;

namespace UnityEngine.ProBuilder
{
    public class Pipe : Shape
    {
        [Min(0.01f)]
        [SerializeField]
        float thickness = .25f;

        [Range(3, 64)]
        [SerializeField]
        int numberOfSlides = 6;

        [Range(0, 32)]
        [SerializeField]
        int heightSeigments = 1;


        public override void RebuildMesh(ProBuilderMesh mesh, Vector3 size)
        {
            var height = size.y;
            var radius = System.Math.Min(size.x, size.z);
            // template is outer ring - radius refers to outer ring always
            Vector2[] templateOut = new Vector2[numberOfSlides];
            Vector2[] templateIn = new Vector2[numberOfSlides];

            for (int i = 0; i < numberOfSlides; i++)
            {
                templateOut[i] = Math.PointInCircumference(radius, i * (360f / numberOfSlides), Vector2.zero);
                templateIn[i] = Math.PointInCircumference(radius - thickness, i * (360f / numberOfSlides), Vector2.zero);
            }

            List<Vector3> v = new List<Vector3>();

            // build out sides
            Vector2 tmp, tmp2, tmp3, tmp4;
            for (int i = 0; i < heightSeigments; i++)
            {
                // height subdivisions
                float y = i * (height / heightSeigments);
                float y2 = (i + 1) * (height / heightSeigments);

                for (int n = 0; n < numberOfSlides; n++)
                {
                    tmp = templateOut[n];
                    tmp2 = n < (numberOfSlides - 1) ? templateOut[n + 1] : templateOut[0];

                    // outside quads
                    Vector3[] qvo = new Vector3[4]
                    {
                        new Vector3(tmp2.x, y, tmp2.y),
                        new Vector3(tmp.x, y, tmp.y),
                        new Vector3(tmp2.x, y2, tmp2.y),
                        new Vector3(tmp.x, y2, tmp.y)
                    };

                    // inside quad
                    tmp = templateIn[n];
                    tmp2 = n < (numberOfSlides - 1) ? templateIn[n + 1] : templateIn[0];
                    Vector3[] qvi = new Vector3[4]
                    {
                        new Vector3(tmp.x, y, tmp.y),
                        new Vector3(tmp2.x, y, tmp2.y),
                        new Vector3(tmp.x, y2, tmp.y),
                        new Vector3(tmp2.x, y2, tmp2.y)
                    };

                    v.AddRange(qvo);
                    v.AddRange(qvi);
                }
            }

            // build top and bottom
            for (int i = 0; i < numberOfSlides; i++)
            {
                tmp = templateOut[i];
                tmp2 = (i < numberOfSlides - 1) ? templateOut[i + 1] : templateOut[0];
                tmp3 = templateIn[i];
                tmp4 = (i < numberOfSlides - 1) ? templateIn[i + 1] : templateIn[0];

                // top
                Vector3[] tpt = new Vector3[4]
                {
                    new Vector3(tmp2.x, height, tmp2.y),
                    new Vector3(tmp.x,  height, tmp.y),
                    new Vector3(tmp4.x, height, tmp4.y),
                    new Vector3(tmp3.x, height, tmp3.y)
                };

                // top
                Vector3[] tpb = new Vector3[4]
                {
                    new Vector3(tmp.x, 0f, tmp.y),
                    new Vector3(tmp2.x, 0f, tmp2.y),
                    new Vector3(tmp3.x, 0f, tmp3.y),
                    new Vector3(tmp4.x, 0f, tmp4.y),
                };

                v.AddRange(tpb);
                v.AddRange(tpt);
            }
            mesh.GeometryWithPoints(v.ToArray());
        }
    }
}
