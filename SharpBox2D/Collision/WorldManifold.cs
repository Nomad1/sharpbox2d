/*******************************************************************************
 * Copyright (c) 2013, Daniel Murphy
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 	* Redistributions of source code must retain the above copyright notice,
 * 	  this list of conditions and the following disclaimer.
 * 	* Redistributions in binary form must reproduce the above copyright notice,
 * 	  this list of conditions and the following disclaimer in the documentation
 * 	  and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/
using SharpBox2D.Common;

namespace SharpBox2D.Collision
{

/**
 * This is used to compute the current state of a contact manifold.
 * 
 * @author daniel
 */

    public class WorldManifold
    {
        /**
   * World vector pointing from A to B
   */
        public Vec2 normal;

        /**
   * World contact point (point of intersection)
   */
        public Vec2[] points;

        /**
   * A negative value indicates overlap, in meters.
   */
        public readonly float[] separations;

        public WorldManifold()
        {
            normal = new Vec2();
            points = new Vec2[Settings.maxManifoldPoints];
            separations = new float[Settings.maxManifoldPoints];
            for (int i = 0; i < Settings.maxManifoldPoints; i++)
            {
                points[i] = new Vec2();
            }
        }

        private readonly Vec2 pool3 = new Vec2();
        private readonly Vec2 pool4 = new Vec2();

        public void initialize(Manifold manifold, Transform xfA, float radiusA,
            Transform xfB, float radiusB)
        {
            if (manifold.pointCount == 0)
            {
                return;
            }
            Vec2 clipPoint;
            Vec2 planePoint;

            switch (manifold.type)
            {
                case Manifold.ManifoldType.CIRCLES:
                {
                    Vec2 pointA = pool3;
                    Vec2 pointB = pool4;

                    normal.x = 1;
                    normal.y = 0;
                    Vec2 v = manifold.localPoint;
                    // Transform.mulToOutUnsafe(xfA, manifold.localPoint, pointA);
                    // Transform.mulToOutUnsafe(xfB, manifold.points[0].localPoint, pointB);
                    pointA.x = (xfA.q.c*v.x - xfA.q.s*v.y) + xfA.p.x;
                    pointA.y = (xfA.q.s*v.x + xfA.q.c*v.y) + xfA.p.y;
                    Vec2 mp0p = manifold.points[0].localPoint;
                    pointB.x = (xfB.q.c*mp0p.x - xfB.q.s*mp0p.y) + xfB.p.x;
                    pointB.y = (xfB.q.s*mp0p.x + xfB.q.c*mp0p.y) + xfB.p.y;

                    if (MathUtils.distanceSquared(pointA, pointB) > Settings.EPSILON*Settings.EPSILON)
                    {
                        normal.x = pointB.x - pointA.x;
                        normal.y = pointB.y - pointA.y;
                        normal.normalize();
                    }

                    float cAx = normal.x*radiusA + pointA.x;
                    float cAy = normal.y*radiusA + pointA.y;

                    float cBx = -normal.x*radiusB + pointB.x;
                    float cBy = -normal.y*radiusB + pointB.y;

                    points[0].x = (cAx + cBx)*.5f;
                    points[0].y = (cAy + cBy)*.5f;
                    separations[0] = (cBx - cAx)*normal.x + (cBy - cAy)*normal.y;
                }
                    break;
                case Manifold.ManifoldType.FACE_A:
                    planePoint = pool3;

                    Rot.mulToOutUnsafe(xfA.q, manifold.localNormal, ref normal);
                    Transform.mulToOut(xfA, manifold.localPoint, ref planePoint);

                    clipPoint = pool4;

                    for (int i = 0; i < manifold.pointCount; i++)
                    {
                        // b2Vec2 clipPoint = b2Mul(xfB, manifold->points[i].localPoint);
                        // b2Vec2 cA = clipPoint + (radiusA - b2Dot(clipPoint - planePoint,
                        // normal)) * normal;
                        // b2Vec2 cB = clipPoint - radiusB * normal;
                        // points[i] = 0.5f * (cA + cB);
                        Transform.mulToOut(xfB, manifold.points[i].localPoint, ref clipPoint);
                        // use cA as temporary for now
                        // cA.set(clipPoint).subLocal(planePoint);
                        // float scalar = radiusA - Vec2.dot(cA, normal);
                        // cA.set(normal).mulLocal(scalar).addLocal(clipPoint);
                        // cB.set(normal).mulLocal(radiusB).subLocal(clipPoint).negateLocal();
                        // points[i].set(cA).addLocal(cB).mulLocal(0.5f);

                        float scalar =
                            radiusA
                            - ((clipPoint.x - planePoint.x)*normal.x + (clipPoint.y - planePoint.y)
                               *normal.y);

                        float cAx = normal.x*scalar + clipPoint.x;
                        float cAy = normal.y*scalar + clipPoint.y;

                        float cBx = -normal.x*radiusB + clipPoint.x;
                        float cBy = -normal.y*radiusB + clipPoint.y;

                        points[i].x = (cAx + cBx)*.5f;
                        points[i].y = (cAy + cBy)*.5f;
                        separations[i] = (cBx - cAx)*normal.x + (cBy - cAy)*normal.y;

                    }
                    break;
                case Manifold.ManifoldType.FACE_B:
                    planePoint = pool3;
                    Rot.mulToOutUnsafe(xfB.q, manifold.localNormal, ref normal);
                    Transform.mulToOut(xfB, manifold.localPoint, ref planePoint);

                    // final Mat22 R = xfB.q;
                    // normal.x = R.ex.x * manifold.localNormal.x + R.ey.x * manifold.localNormal.y;
                    // normal.y = R.ex.y * manifold.localNormal.x + R.ey.y * manifold.localNormal.y;
                    // final Vec2 v = manifold.localPoint;
                    // planePoint.x = xfB.p.x + xfB.q.ex.x * v.x + xfB.q.ey.x * v.y;
                    // planePoint.y = xfB.p.y + xfB.q.ex.y * v.x + xfB.q.ey.y * v.y;

                    clipPoint = pool4;

                    for (int i = 0; i < manifold.pointCount; i++)
                    {
                        // b2Vec2 clipPoint = b2Mul(xfA, manifold->points[i].localPoint);
                        // b2Vec2 cB = clipPoint + (radiusB - b2Dot(clipPoint - planePoint,
                        // normal)) * normal;
                        // b2Vec2 cA = clipPoint - radiusA * normal;
                        // points[i] = 0.5f * (cA + cB);

                        Transform.mulToOut(xfA, manifold.points[i].localPoint, ref clipPoint);
                        // cB.set(clipPoint).subLocal(planePoint);
                        // float scalar = radiusB - Vec2.dot(cB, normal);
                        // cB.set(normal).mulLocal(scalar).addLocal(clipPoint);
                        // cA.set(normal).mulLocal(radiusA).subLocal(clipPoint).negateLocal();
                        // points[i].set(cA).addLocal(cB).mulLocal(0.5f);

                        // points[i] = 0.5f * (cA + cB);

                        //
                        // clipPoint.x = xfA.p.x + xfA.q.ex.x * manifold.points[i].localPoint.x + xfA.q.ey.x *
                        // manifold.points[i].localPoint.y;
                        // clipPoint.y = xfA.p.y + xfA.q.ex.y * manifold.points[i].localPoint.x + xfA.q.ey.y *
                        // manifold.points[i].localPoint.y;

                        float scalar =
                            radiusB
                            - ((clipPoint.x - planePoint.x)*normal.x + (clipPoint.y - planePoint.y)
                               *normal.y);

                        float cBx = normal.x*scalar + clipPoint.x;
                        float cBy = normal.y*scalar + clipPoint.y;

                        float cAx = -normal.x*radiusA + clipPoint.x;
                        float cAy = -normal.y*radiusA + clipPoint.y;

                        points[i].x = (cAx + cBx)*.5f;
                        points[i].y = (cAy + cBy)*.5f;
                        separations[i] = (cAx - cBx)*normal.x + (cAy - cBy)*normal.y;
                    }
                    // Ensure normal points from A to B.
                    normal.x = -normal.x;
                    normal.y = -normal.y;
                    break;
            }
        }
    }
}