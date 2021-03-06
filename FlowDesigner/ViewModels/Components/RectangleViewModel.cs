﻿using System;
using System.Linq;
using System.Numerics;
using Aptacode.CSharp.Common.Utilities.Extensions;
using Aptacode.FlowDesigner.Core.Enums;
using Aptacode.PathFinder.Geometry.Neighbours;

namespace Aptacode.FlowDesigner.Core.ViewModels.Components
{
    public class RectangleViewModel : PolygonViewModel
    {
        private Vector2 _size;

        public RectangleViewModel(Guid id, Vector2 position, Vector2 size) : base(id, position)
        {
            _size = size;
            _points = new Vector2[]
            {
                TopLeft,
                 TopRight,
                 BottomRight,
                 BottomLeft
            }; 
            Size = size;

        }
        #region Properties

        public Vector2 Position
        {
            get => _points[0];
            set
            {
                _points[0] = value;
                SetProperty(ref _position, value);
            }
        }

        public Vector2 Size
        {
            get => _points.ElementAt(2) - _points.First();
            set
            {
                //Todo
                SetProperty(ref _size, value);
            }
        }

        public Vector2 MidPoint => Position + _size / 2;

        public Vector2 TopLeft => Position;
        public Vector2 TopRight => Position + _size * new Vector2(1, 0);
        public Vector2 BottomRight => Position + _size;
        public Vector2 BottomLeft => Position + _size * new Vector2(0, 1);

        public Vector2 PositionAndMargin => Position - new Vector2(Margin, Margin);
        public Vector2 SizeAndMargin => _size + new Vector2(Margin, Margin);
        #endregion

        #region Collisions

        #endregion


        public ResizeDirection GetCollidingEdge(Vector2 position)
        {
            if (position == TopLeft)
            {
                return ResizeDirection.NW;
            }

            if (position == TopRight)
            {
                return ResizeDirection.NE;
            }

            if (position == BottomLeft)
            {
                return ResizeDirection.SW;
            }

            if (position == BottomRight)
            {
                return ResizeDirection.SE;
            }

            if (Math.Abs(position.X - TopLeft.X) < Constants.Tolerance)
            {
                return ResizeDirection.W;
            }

            if (Math.Abs(position.X - TopRight.X) < Constants.Tolerance)
            {
                return ResizeDirection.E;
            }

            if (Math.Abs(position.Y - TopLeft.Y) < Constants.Tolerance)
            {
                return ResizeDirection.N;
            }

            if (Math.Abs(position.Y - BottomLeft.Y) < Constants.Tolerance)
            {
                return ResizeDirection.S;
            }

            return ResizeDirection.None;
        }

        public override void Resize(DesignerViewModel designer, Vector2 delta, ResizeDirection direction)
        {
            var newPosition = Position;
            var newSize = Size;
            switch (direction)
            {
                case ResizeDirection.NW:
                    newPosition += delta;
                    newSize += delta * new Vector2(-1, -1);
                    break;
                case ResizeDirection.NE:
                    newPosition += delta * new Vector2(0, 1);
                    newSize += delta * new Vector2(1, -1);
                    break;
                case ResizeDirection.SE:
                    newSize += delta;
                    break;
                case ResizeDirection.SW:
                    newPosition += delta * new Vector2(1, 0);
                    newSize += delta * new Vector2(-1, 1);
                    break;
                case ResizeDirection.N:
                    newPosition += delta * new Vector2(0, 1);
                    newSize += delta * new Vector2(0, -1);
                    break;
                case ResizeDirection.S:
                    newSize += delta * new Vector2(0, 1);
                    break;
                case ResizeDirection.E:
                    newSize += delta * new Vector2(1, 0);
                    break;
                case ResizeDirection.W:
                    newPosition += delta * new Vector2(1, 0);
                    newSize += delta * new Vector2(-1, 0);
                    break;
                case ResizeDirection.None:
                default:
                    break;
            }

            if (!(newSize.X >= 2) || !(newSize.Y >= 2) ||
                designer.ConnectedComponents.Count(i => i.CollidesWith(CollisionType.Margin, Points.ToArray())) > 1)
            {
                return;
            }

            Position = newPosition;
            Size = newSize;
        }

        public override void AddTo(DesignerViewModel designer)
        {
            designer.Add(this);
        }

        public override void RemoveFrom(DesignerViewModel designer)
        {
            designer.Add(this);
        }

        public override void Resize(DesignerViewModel designer, Vector2 delta)
        {
            Size += delta;
        }
    }
}