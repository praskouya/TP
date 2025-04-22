//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Windows;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace TP.ConcurrentProgramming.Data
{
  internal class DataImplementation : DataAbstractAPI
  {
    #region ctor

    public DataImplementation()
    {
      MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    #endregion ctor

    #region DataAbstractAPI

    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      Random random = new Random();
      for (int i = 0; i < numberOfBalls; i++)
      {
                Ball newBall;
                Vector startingPosition;
                Vector startingVelocity;
                Vector predictDelta = new(0, 0);
                do
                {
                    startingPosition = new(random.Next(100, 400 - 100), random.Next(100, 400 - 100));
                    startingVelocity = new((random.NextDouble() - 0.5) * 6, (random.NextDouble() - 0.5) * 6);
                    newBall = new(startingPosition, startingVelocity); 
                } while (CheckCollision(startingPosition, newBall) != -1);

        upperLayerHandler(startingPosition, newBall);
        BallsList.Add(newBall);
      }
    }

        public override void Stop()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            MoveTimer.Dispose(); // Stop the timer
            BallsList.Clear();   // Clear the list of balls

           Environment.Exit(0); // Close the application
        }


        #endregion DataAbstractAPI

        #region IDisposable

        protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          MoveTimer.Dispose();
          BallsList.Clear();
        }
        Disposed = true;
      }
      else
        throw new ObjectDisposedException(nameof(DataImplementation));
    }

    public override void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

        #endregion IDisposable

        #region private
        
            //private bool disposedValue;
            private bool Disposed = false;

            private readonly Timer MoveTimer;
            private Random RandomGenerator = new();
            private List<Ball> BallsList = [];

            private const int margin = 4;
            private const double ballDiameter = 20.0;
            private const double width = 400.0;
            private const double height = 420.0;

                private int CheckCollision(Vector targetPos, Ball current)
                {
                    // Sprawdzenie kolizji ze ścianami po osi X
                    if (targetPos.x <= 0 - margin / 2 || targetPos.x + ballDiameter >= width - 2 * margin)
                        return -2;

                    // Sprawdzenie kolizji ze ścianami po osi Y
                    if (targetPos.y <= 0 - margin / 2 || targetPos.y + ballDiameter >= height - 2 * margin)
                        return -3;

                    // Sprawdzenie kolizji z innymi kulami
                    foreach (var other in BallsList)
                    {
                        if (other == current)
                            continue;

                        double dx = other.PositionValue.x - targetPos.x;
                        double dy = other.PositionValue.y - targetPos.y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        if (dist <= ballDiameter)
                            return BallsList.IndexOf(other);
                    }

                    return -1; // Brak kolizji
                }


                private void Move(object? state)
                {
                    const int maxAttempts = 50;

                    foreach (Ball ball in BallsList)
                    {
                        int attempts = 0;
                        while (true)
                        {
                            Vector velocity = (Vector)ball.Velocity;
                            Vector proposedPosition = new(ball.PositionValue.x + velocity.x, ball.PositionValue.y + velocity.y);

                            int collisionCode = CheckCollision(proposedPosition, ball);

                            switch (collisionCode)
                            {
                                case -1:
                                    ball.Move(velocity);
                                    goto NextBall;

                                case -2:
                                    ball.Velocity = new Vector(-velocity.x, velocity.y);
                                    break;

                                case -3:
                                    ball.Velocity = new Vector(velocity.x, -velocity.y);
                                    break;

                                default:
                                    Ball other = BallsList[collisionCode];

                                    Vector pos1 = (Vector)ball.PositionValue;
                                    Vector pos2 = (Vector)other.PositionValue;
                                    Vector vel1 = (Vector)ball.Velocity;
                                    Vector vel2 = (Vector)other.Velocity;

                                    double dx = pos1.x - pos2.x;
                                    double dy = pos1.y - pos2.y;
                                    double dist = Math.Sqrt(dx * dx + dy * dy);
                                    if (dist == 0) dist = 0.01;

                                    double normX = dx / dist;
                                    double normY = dy / dist;

                                    double proj1 = vel1.x * normX + vel1.y * normY;
                                    double proj2 = vel2.x * normX + vel2.y * normY;
                                    double impulse = proj1 - proj2;

                                    ball.Velocity = new Vector(vel1.x - impulse * normX, vel1.y - impulse * normY);
                                    other.Velocity = new Vector(vel2.x + impulse * normX, vel2.y + impulse * normY);
                                    break;
                            }

                            if (++attempts >= maxAttempts)
                                break;
                        }

                    NextBall:
                        continue;
                    }
                } 


        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
    {
      returnBallsList(BallsList);
    }

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
    {
      returnNumberOfBalls(BallsList.Count);
    }

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}