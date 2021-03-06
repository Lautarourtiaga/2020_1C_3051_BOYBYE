﻿using TGC.Core.Mathematica;
using TGC.Core.Text;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Collision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace TGC.Group.Model
{
    public class Nave : IRenderizable
    {
        private TGCVector3 posicion; 
        private readonly ModeloCompuesto modeloNave;
        private readonly InputDelJugador input;
        private readonly float velocidadBase;
        private float velocidadActual;
        private readonly float aceleracionMovimiento;
        private TGCVector3 rotacionBase;
        private TGCVector3 rotacionActual;
        private readonly float velocidadRotacion;
        private bool estaRolleando;
        private bool estaVivo;
        private TgcText2D textoGameOver;
        private string mediaDir;
        private DateTime ultimoRoll;

        public Nave(string mediaDir, TGCVector3 posicionInicial, InputDelJugador input)
        {
            this.mediaDir = mediaDir;
            this.modeloNave = new ModeloCompuesto(mediaDir + "XWing\\X-Wing-TgcScene.xml", posicion);
            this.posicion = posicionInicial;
            this.input = input;
            this.velocidadBase = 5f;
            this.velocidadActual = velocidadBase;
            this.aceleracionMovimiento = 0.01f;
            this.rotacionBase = new TGCVector3(0f, Geometry.DegreeToRadian(180f), 0f);
            this.rotacionActual = rotacionBase;
            this.velocidadRotacion = 0.008f;
            this.estaRolleando = false;
            this.estaVivo = true;
            this.ultimoRoll = new DateTime(0);
        }


        public void Init()
        {
            
            TGCVector3 rotacionInicial = new TGCVector3(0f, 1f, 0f) * Geometry.DegreeToRadian(180f);
            modeloNave.CambiarRotacion(rotacionInicial);
            SetearTextoGameOver();
        }

        public void Update(float elapsedTime)
        {

            if (input.HayInputDeAceleracion())
            {
                float aceleracionDelInput = input.SentidoDeAceleracionDelInput() * aceleracionMovimiento;
                Acelerar(aceleracionDelInput);
            }
            else
            {
                VolverAVelocidadNormal();
            }

            if (input.HayInputDeRoll())
            {
                EmpezarARollear();
            }

            if (estaRolleando)
            {
                Rollear();
            }
            else
            {
                if (input.HayInputDeRotacion())
                {
                    RotarEnDireccion(input.RotacionDelInput());
                }
                else
                {
                    VolverARotacionNormal();
                }
            }
            if (input.HayInputDeDisparo())
            {
                Disparar();
            }

            MoverseEnDireccion(input.DireccionDelInput(), elapsedTime);

        }

        public void Render()
        {
            if (estaVivo)
            {
                modeloNave.AplicarTransformaciones();
                modeloNave.Render();
            }
            else
            {
                textoGameOver.render();
            }

            new TgcText2D().drawText("Velocidad de la nave:\n" + velocidadActual.ToString(), 5, 20, Color.White);
            new TgcText2D().drawText("Posicion de la nave:\n" + posicion.ToString(), 5, 60, Color.White);
            new TgcText2D().drawText("Rotacion de la nave:\n" + rotacionActual.ToString(), 5, 130, Color.White);

        }

        public void Dispose()
        {
            modeloNave.Dispose();
        }
        public TGCVector3 GetPosicion()
        {
            return posicion;
        }

        #region Movimientos

        private void MoverseEnDireccion(TGCVector3 versorDirector,float elapsedTime)
        {
            TGCVector3 movimientoDelFrame = new TGCVector3(0, 0, 0);
            TGCVector3 movimientoAdelante = new TGCVector3(0, 0, 1);

            movimientoDelFrame += versorDirector + movimientoAdelante;
            movimientoDelFrame *= 10f * elapsedTime * velocidadActual;

            posicion += movimientoDelFrame;

            modeloNave.CambiarPosicion(posicion);
        }
        private void RotarEnDireccion(TGCVector3 versorDirector)
        {
            float limiteEnZ = Geometry.DegreeToRadian(20f);
            float limiteEnX = Geometry.DegreeToRadian(15f);
            TGCVector3 rotacionASumar = versorDirector * velocidadRotacion;

            float nuevaRotacionZ = NuevaRotacionEnEjeSegunLimite(rotacionASumar.Z + rotacionActual.Z, limiteEnZ);
            float nuevaRotacionX = NuevaRotacionEnEjeSegunLimite(rotacionASumar.X + rotacionActual.X, limiteEnX);

            Rotar(new TGCVector3(nuevaRotacionX, Geometry.DegreeToRadian(180f), nuevaRotacionZ));
        }

        private void Rotar(TGCVector3 nuevaRotacion)
        {
            rotacionActual = nuevaRotacion;
            modeloNave.CambiarRotacion(rotacionActual);
        }
        private void Acelerar(float aceleracion)
        {
            float velocidadMaxima = velocidadBase * 4;
            float velocidadMinima = velocidadBase / 2;

            float nuevaVelocidad = aceleracion + velocidadActual;

            if (nuevaVelocidad < velocidadMinima)
            {
                velocidadActual -= aceleracion;
            }
            else if(nuevaVelocidad > velocidadMaxima)
            {
                velocidadActual -= aceleracion;
            }
            else
            {
                velocidadActual = nuevaVelocidad;
            }
        }

        private void VolverARotacionNormal()
        {
            TGCVector3 direccionDeRotacionNecesaria = TGCVector3.Normalize(rotacionBase - rotacionActual);
            RotarEnDireccion(direccionDeRotacionNecesaria);
        }
        private void VolverAVelocidadNormal()
        {
            float aceleracion = Math.Sign(velocidadBase - velocidadActual) * aceleracionMovimiento;
            Acelerar(aceleracion);
        }
        private float NuevaRotacionEnEjeSegunLimite(float nuevaPosibleRotacion, float rotacionLimite) //Mal nombre aaa
        {
            float rotacionMaxima = rotacionLimite;
            float rotacionMinima = -rotacionLimite;

            if (nuevaPosibleRotacion >= rotacionMaxima)
            {
                return rotacionMaxima;
            }
            else if (nuevaPosibleRotacion <= rotacionMinima)
            {
                return rotacionMinima;
            }
            else
            {
                return nuevaPosibleRotacion;
            }
        }
        #endregion

        #region Roll
        private void EmpezarARollear()
        {
            if (SePuedeRollear())
            {
                estaRolleando = true;
                ultimoRoll = DateTime.Now;
            }
        }

        private Boolean SePuedeRollear()
        {
            return (DateTime.Now - ultimoRoll).TotalSeconds > 30;
        }

        private void Rollear()
        {
            Rotar(rotacionActual + new TGCVector3(0, 0, aceleracionMovimiento*4));

            if (TerminoElRoll())
            {
                estaRolleando = false;
            }
        }

        private bool TerminoElRoll()
        {
            return rotacionActual.Z > Math.PI*2;
        }
        #endregion
        private void SetearTextoGameOver()
        {
            textoGameOver = new TgcText2D
            {
                Text = "GAME\nOVER",
                Color = Color.Black,
                Align = TgcText2D.TextAlign.CENTER,
                Position = new Point(500, 300),
                Size = new Size(400, 200)
            };
            textoGameOver.changeFont(new System.Drawing.Font("TimesNewRoman", 100, FontStyle.Bold | FontStyle.Italic));
        }
        private void Disparar()
        {
            TGCVector3 posicionLaser = new TGCVector3(GetPosicion());
            //posicionLaser.Z += 100f;
            GameManager.Instance.AgregarRenderizable(new LaserDeJugador(mediaDir, posicionLaser, new TGCVector3(0,0,1),this));

        }

        public Boolean ColisionaConColisionable(Colisionable unColisionable)
        {
            return modeloNave.ColisionaConColisionable(unColisionable);
        }

        public void Morir()
        {
            estaVivo = false;
        }

        public void Colisionar()
        {
            Morir();
        }

        public void ChocarConLaser()
        {
            if (!estaRolleando)
            {
                Morir();
            }
        }
    }
}

