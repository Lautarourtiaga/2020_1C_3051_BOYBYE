﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX.Direct3D;
using TGC.Core.BoundingVolumes;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;

namespace TGC.Group.Model
{
    public abstract class Laser : Colisionable
    {
        internal string direccionDeScene;
        private readonly TGCVector3 posicionInicial;
        private readonly TGCVector3 direccion;

        private TGCMatrix baseScaleRotation;
        private TGCMatrix baseQuaternionTranslation;

        public Laser(string mediaDir, TGCVector3 posicionInicial,TGCVector3 direccion, Nave naveDelJugador) :  base(naveDelJugador)
        {
            this.posicionInicial = posicionInicial;
            this.direccion = direccion;
        }



        public override void Init()
        {
            TgcSceneLoader loader = new TgcSceneLoader();
            TgcScene scene2 = loader.loadSceneFromFile(direccionDeScene);

            //Solo nos interesa el primer modelo de esta escena (tiene solo uno)
            mainMesh = scene2.Meshes[0];
            mainMesh.Position = posicionInicial;
            baseQuaternionTranslation = TGCMatrix.Translation(posicionInicial);
            baseScaleRotation = TGCMatrix.Scaling(new TGCVector3(.2f, .2f, .2f));
            mainMesh.Transform = TGCMatrix.Scaling(0.1f, 0.1f, 0.1f) * TGCMatrix.Translation(mainMesh.Position);
        }

        public override void Update(float elapsedTime)
        {
            TGCQuaternion rotation = TGCQuaternion.RotationAxis(new TGCVector3(1.0f, 0.0f, 0.0f), Geometry.DegreeToRadian(90f));
            TGCVector3 direccionDisparo = direccion;
            direccionDisparo.Normalize();
            TGCQuaternion giro = QuaternionDireccion(direccionDisparo);
            TGCVector3 movement = direccionDisparo * 60f * elapsedTime;
            mainMesh.Position += movement;
            TGCMatrix matrizTransformacion = baseScaleRotation * TGCMatrix.RotationTGCQuaternion(rotation*giro)
                * TGCMatrix.Translation(mainMesh.Position);
            mainMesh.Transform = matrizTransformacion;
            mainMesh.BoundingBox.transform(matrizTransformacion);
        }

        public override void Render()
        {
            mainMesh.Render();
            mainMesh.BoundingBox.Render();

        }
        public override void Dispose()
        {
            mainMesh.Dispose();
        }

        private TGCQuaternion QuaternionDireccion(TGCVector3 direccionDisparoNormalizado)
        {
            TGCVector3 DireccionA = new TGCVector3(0, 0, -1);
            TGCVector3 cross = TGCVector3.Cross(DireccionA, direccionDisparoNormalizado);
            TGCQuaternion newRotation = TGCQuaternion.RotationAxis(cross, FastMath.Acos(TGCVector3.Dot(DireccionA, direccionDisparoNormalizado)));
            return newRotation;
        }
    }
}
