﻿﻿//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Threading;
using System.Diagnostics;
using System.Numerics;
using System.Reactive;
using System.Runtime.InteropServices;
using TP.ConcurrentProgramming.Data;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
    {
    #region ctor

    public BusinessLogicImplementation() : this(null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer ?? UnderneathLayerAPI.GetDataLayer();
            layerBellow.SetPositionValidator(pos => IsValidPosition(new Position(pos.x, pos.y)));

            Observer = layerBellow.Subscribe(new 
                AnonymousObserver<BallChaneEventArgs>(x => 
                    CheckColision(x.Ball, new Position(x.Pos.x, x.Pos.y))));
        }

    #endregion ctor

    #region BusinessLogicAbstractAPI

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      layerBellow.Dispose();
      Disposed = true;
    }


    // public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
    // {
    //   if (Disposed)
    //     throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
    //   if (upperLayerHandler == null)
    //     throw new ArgumentNullException(nameof(upperLayerHandler));
    //         _upperLayerHandler = upperLayerHandler;
    //  
    //   layerBellow.Start(numberOfBalls, (startingPosition, databall) => {
    //       Ball buf = new Ball(databall, new Position (startingPosition.x, startingPosition.y));
    //       _ballList.Add(buf);
    //       upperLayerHandler(new Position(startingPosition.x, startingPosition.y), buf);
    //   });
    // }
    
    public override void Start(int numberOfBalls, Action<IPosition, IBall> upperLayerHandler)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
        if (upperLayerHandler == null)
            throw new ArgumentNullException(nameof(upperLayerHandler));
        layerBellow.Start(numberOfBalls, (startingPosition, 
            databall) => upperLayerHandler(new 
            Position(startingPosition.x, startingPosition.x), 
            new Ball(databall, new Position (startingPosition.x, startingPosition.y))));
    }

    public override void Stop()
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
        layerBellow.Stop(); // Delegate the stop logic to the data layer
    }
        public override void UpdateBallsCount(int numberofBalls, Action<IPosition, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
            if (_upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));
            _ballList.Clear();
            layerBellow.UpdateBallsCount(numberofBalls, (startingPosition, databall) =>
            {
                Ball buf = new Ball(databall, new Position(startingPosition.x, startingPosition.y));
                _ballList.Add(buf);
                _upperLayerHandler(new Position(startingPosition.x, startingPosition.y), new Ball(databall, new Position(startingPosition.x, startingPosition.y)));
            });
        }
        
        #endregion BusinessLogicAbstractAPI

        #region private

        private readonly object  _lock = new();

        private IDisposable Observer = null;
        private IDisposable PositionObserver = null;

        private bool Disposed = false;

        private Action<IPosition, IBall>? _upperLayerHandler;

        private readonly UnderneathLayerAPI layerBellow;

        private readonly List<Ball> _ballList = new();

        private double width = 400;
        private double height = 420;
        private int margin = 4;
        private int ballDiameter = 20;

        private void CheckColision(Data.IBall Item, IPosition Pos)
        {
            lock (_lock)
            {
                int indx = -1;
                IPosition newPos = new Position(Pos.x + Item.Velocity.x, Pos.y + Item.Velocity.y);

                if ((newPos.x <= 0 - margin / 2) | (newPos.x + ballDiameter >= width - margin * 2))
                {
                    Item.SetVelocity(-Item.Velocity.x, Item.Velocity.y);
                }
                else if ((newPos.y <= 0 - margin / 2) | (newPos.y + ballDiameter >= height - margin * 2))
                {
                    Item.SetVelocity(Item.Velocity.x, -Item.Velocity.y);
                }
                else
                {
                    double distance;
                    for (int i = 0; i < _ballList.Count; i++)
                    {
                        Ball others = _ballList[i];
                        if (others.PositionValue.x == Pos.x && others.PositionValue.y == Pos.y)
                        {
                            continue;
                        }

                        distance = Math.Sqrt(Math.Pow(others.PositionValue.x - newPos.x, 2) + Math.Pow((others.PositionValue.y - newPos.y), 2));
                        if (distance <= ballDiameter)
                        {
                            indx = _ballList.IndexOf(others);
                            break;
                        }
                    }
                    if (indx == -1) return;
                    else
                    {
                        Ball B = _ballList[indx];

                        IPosition posB = B.PositionValue;
                        IVector velB = B.Velocity;

                        double dx = Pos.x - posB.x;
                        double dy = Pos.y - posB.y;
                        distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance == 0) distance = 0.01;

                        double nx = dx / distance;
                        double ny = dy / distance;

                        double vA_proj = Item.Velocity.x * nx + Item.Velocity.y * ny;
                        double vB_proj = velB.x * nx + velB.y * ny;
                        double impulse = vA_proj - vB_proj;

                        Item.SetVelocity(Item.Velocity.x - impulse * nx, Item.Velocity.y - impulse * ny);
                        B.setVelocity(velB.x + impulse * nx, velB.y + impulse * ny);
                    }
                }
            }
        }

        public bool IsValidPosition(IPosition position)
        {
            foreach (var ball in _ballList)
            {
                double dx = ball.PositionValue.x - position.x;
                double dy = ball.PositionValue.y - position.y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= ballDiameter)
                    return false;
            }

            return position.x >= 0 && position.x + ballDiameter <= width - margin &&
                   position.y >= 0 && position.y + ballDiameter <= height - margin;
        }


        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}