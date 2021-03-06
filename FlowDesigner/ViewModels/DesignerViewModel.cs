﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
using Aptacode.CSharp.Common.Utilities.Mvvm;
using Aptacode.FlowDesigner.Core.Enums;
using Aptacode.FlowDesigner.Core.Extensions;
using Aptacode.FlowDesigner.Core.Utilities;
using Aptacode.FlowDesigner.Core.ViewModels.Components;

namespace Aptacode.FlowDesigner.Core.ViewModels
{
    public class DesignerViewModel : BindableBase
    {
        public DesignerViewModel(int width, int height)
        {
            Width = width;
            Height = height;
            Selection = new SelectionViewModel(Guid.NewGuid(), Vector2.Zero, Vector2.Zero);
            CreateSelection = new SelectionViewModel(Guid.NewGuid(), Vector2.Zero, Vector2.Zero)
            {
                BorderColor = Color.Green
            };

            NewConnectionPath = new PathViewModel();
            MousePoint = new PointViewModel(Guid.NewGuid(), Vector2.Zero) {IsShown = false};

            ResizeDirection = ResizeDirection.None;

            Interactor.AddPoint += UserInteractionManager_AddPoint;
            Interactor.CreateAt += UserInteractionManager_CreateAt;
            Interactor.DeleteAt += UserInteractionManager_DeleteAt;
            Interactor.SelectAt += UserInteractionManager_SelectAt;
            Interactor.MouseMoved += UserInteractionManager_MouseMoved;
            Interactor.MouseReleased += UserInteractionManager_MouseReleased;

            Interactor.MouseClicked += Interactor_MouseClicked;
            Interactor.MouseDoubleClicked += Interactor_MouseDoubleClicked;
            Interactor.KeyPressed += Interactor_KeyPressed;
        }

        public UserInteractionManager Interactor { get; set; } = new UserInteractionManager();

        #region Connections

        public void RedrawConnections()
        {
            Connections.ToList().ForEach(c => c.Redraw());
        }

        #endregion

        #region Components

        public SelectionViewModel Selection { get; set; }
        public SelectionViewModel CreateSelection { get; set; }
        public PathViewModel NewConnectionPath { get; set; }
        public PointViewModel MousePoint { get; set; }
        public Dictionary<Type, List<ComponentViewModel>> ComponentsByType { get; set; } = new Dictionary<Type, List<ComponentViewModel>>();
        public readonly HashSet<ComponentViewModel> SelectedItems = new HashSet<ComponentViewModel>();

        private readonly List<ConnectionViewModel> _connections = new List<ConnectionViewModel>();
        private readonly List<ComponentViewModel> _components = new List<ComponentViewModel>();

        public IEnumerable<ConnectedComponentViewModel> ConnectedComponents =>
            GetComponents<ConnectedComponentViewModel>();

        public IEnumerable<ConnectionPointViewModel> ConnectionPoints => GetComponents<ConnectionPointViewModel>();
        public IEnumerable<PointViewModel> Points => GetComponents<PointViewModel>();
        public IEnumerable<RectangleViewModel> Rectangles => GetComponents<RectangleViewModel>();
        public IEnumerable<PathViewModel> Paths => GetComponents<PathViewModel>();
        public IEnumerable<ConnectionViewModel> Connections => _connections;
        public IEnumerable<ComponentViewModel> Components => _components;

        public IEnumerable<TType> GetComponents<TType>() where TType : ComponentViewModel
        {
            return ComponentsByType.TryGetValue(typeof(TType), out var components) ? components.Select(s => (TType) Convert.ChangeType(s, typeof(TType))) : new List<TType>();
        }

        public void Add<TType>(TType component) where TType : ComponentViewModel
        {
            if (!ComponentsByType.TryGetValue(typeof(TType), out var components))
            {
                components = new List<ComponentViewModel>();
            }

            if (components.Contains(component))
            {
                return;
            }

            components.Add(component);
            _components.Add(component);

            ComponentsByType[typeof(TType)] = components;
            OnPropertyChanged(nameof(Components));
        }

        public void Remove<TType>(TType component) where TType : ComponentViewModel
        {
            if (!ComponentsByType.TryGetValue(typeof(TType), out var components) || !components.Remove(component))
            {
                return;
            }

            _components.Remove(component);
            ComponentsByType[typeof(TType)] = components;
            OnPropertyChanged(nameof(Components));
        }

        public void Add(ConnectionViewModel connection)
        {
            if (_connections.Contains(connection))
            {
                return;
            }

            _connections.Add(connection);
            OnPropertyChanged(nameof(Connections));
        }

        public void Remove(ConnectionViewModel connection)
        {
            _connections.Remove(connection);
            OnPropertyChanged(nameof(Connections));
        }
        #endregion

        #region Properties

        public int Width { get; set; }
        public int Height { get; set; }

        private ResizeDirection _resizingItem;

        public ResizeDirection ResizeDirection
        {
            get => _resizingItem;
            set => SetProperty(ref _resizingItem, value);
        }

        private bool _movingItem;

        public bool MovingItem
        {
            get => _movingItem;
            set => SetProperty(ref _movingItem, value);
        }

        private string _cursor;

        public string Cursor
        {
            get => _cursor;
            set => SetProperty(ref _cursor, value);
        }

        public void UpdateResizeCursor(ResizeDirection direction)
        {
            switch (direction)
            {
                case ResizeDirection.N:
                    Cursor = "n-resize";
                    break;
                case ResizeDirection.NE:
                    Cursor = "ne-resize";
                    break;
                case ResizeDirection.NW:
                    Cursor = "nw-resize";
                    break;
                case ResizeDirection.SE:
                    Cursor = "se-resize";
                    break;
                case ResizeDirection.SW:
                    Cursor = "sw-resize";
                    break;
                case ResizeDirection.S:
                    Cursor = "s-resize";
                    break;
                case ResizeDirection.E:
                    Cursor = "e-resize";
                    break;
                case ResizeDirection.W:
                    Cursor = "w-resize";
                    break;
                case ResizeDirection.None:
                default:
                    Cursor = "auto";
                    break;
            }
        }

        #endregion

        #region Layering

        public void BringToFront(ComponentViewModel component)
        {
            if (!_components.Remove(component))
            {
                return;
            }

            _components.Insert(0, component);
            OnPropertyChanged(nameof(Components));
        }

        public void SendToBack(ComponentViewModel component)
        {
            if (!_components.Remove(component))
            {
                return;
            }

            _components.Add(component);
            OnPropertyChanged(nameof(Components));
        }

        public void BringForward(ComponentViewModel component)
        {
            var index = _components.IndexOf(component);
            if (index == 0)
            {
                return;
            }

            _components.RemoveAt(index);
            _components.Insert(index - 1, component);
            OnPropertyChanged(nameof(Components));
        }

        public void SendBackward(ComponentViewModel component)
        {
            var index = _components.IndexOf(component);
            if (index == _components.Count - 1)
            {
                return;
            }

            _components.RemoveAt(index);
            _components.Insert(index + 1, component);
            OnPropertyChanged(nameof(Components));
        }

        #endregion

        #region Events

        public event EventHandler<IEnumerable<ComponentViewModel>> SelectedItemChanged;
        public event EventHandler<ConnectionPointViewModel> SelectedConnectionChanged;

        #endregion

        #region Interactions

        private void UserInteractionManager_MouseReleased(object sender, Vector2 e)
        {
            if (SelectedItems.Count > 0)
            {
                ItemReleased();
            }

            if (_selectedConnectionPoint != null)
            {
                if (NewConnectionPath.IsShown)
                {
                    ReleaseNewConnectionPath(e);
                }

                ConnectionReleased(e);
            }

            if (Selection.IsShown)
            {
                SelectionReleased();
            }

            if (CreateSelection.IsShown)
            {
                CreateSelectionReleased();
            }

            Cursor = "auto";
        }

        private void UserInteractionManager_MouseMoved(object sender, Vector2 e)
        {
            if (SelectedItems.Count > 0)
            {
                if (MovingItem)
                {
                    Cursor = "move";
                    MoveComponent(e);
                }
                else if (ResizeDirection != ResizeDirection.None)
                {
                    UpdateResizeCursor(ResizeDirection);
                    ResizeItem(e);
                }
            }

            if (_selectedConnectionPoint != null)
            {
                Cursor = "grabbing";
                MoveConnection(e);
            }

            if (Selection.IsShown)
            {
                Cursor = "grabbing";
                Selection.Adjust(Interactor.MouseDownPosition, e);
            }

            if (CreateSelection.IsShown)
            {
                Cursor = "crosshair";
                CreateSelection.Adjust(Interactor.MouseDownPosition, e);
            }

            if(SelectedItems.Count == 0 && _selectedConnectionPoint == null)
            {
                var selectedComponent = ConnectedComponents.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
                if (selectedComponent != null)
                {
                    var resizeDirection = selectedComponent?.GetCollidingEdge(e) ?? ResizeDirection.None;
                    if(resizeDirection != ResizeDirection.None)
                    {
                        UpdateResizeCursor(resizeDirection);
                    }
                    else
                    {
                        if(selectedComponent.ConnectionPoints.Any(c => c.CollidesWith(CollisionType.Normal, e))){
                            Cursor = "grab";
                        }
                        else
                        {
                            Cursor = "move";
                        }
                    }
                }

            }
        }

        private void UserInteractionManager_SelectAt(object sender, Vector2 e)
        {
            var selectedComponent = ConnectedComponents.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
            var selectedComponentPoint = ConnectionPoints.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));

            if (selectedComponentPoint != null)
            {
                SelectedConnectionPoint = selectedComponentPoint;
            }
            else if (selectedComponent != null)
            {
                ClickComponent(selectedComponent, e);
            }
            else
            {
                StartSelection(e);
            }
        }

        private void UserInteractionManager_DeleteAt(object sender, Vector2 e)
        {
            var selectedComponent = ConnectedComponents.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
            var selectedComponentPoint = ConnectionPoints.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
            var selectedConnection = Connections.FirstOrDefault(c => c.Path.CollidesWith(CollisionType.Normal, e));

            if (selectedComponentPoint == null)
            {
                selectedComponent?.RemoveFrom(this);
                selectedConnection?.RemoveFrom(this);
            }
            else
            {
                selectedComponentPoint?.RemoveFrom(this);
            }
        }

        private void UserInteractionManager_CreateAt(object sender, Vector2 e)
        {
            var selectedComponent = ConnectedComponents.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
            var selectedComponentPoint = ConnectionPoints.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));

            if (selectedComponentPoint != null)
            {
                SelectedConnectionPoint = selectedComponentPoint;
            }
            else if (selectedComponent != null)
            {
                SelectedConnectionPoint = this.CreateConnectionPoint(selectedComponent, e);
            }
            else
            {
                StartCreateSelection(e);
            }
        }

        private void UserInteractionManager_AddPoint(object sender, Vector2 position)
        {
            var selectedPoint = Points.FirstOrDefault(p => p.Position == position);
            if (selectedPoint == null)
            {
                var newPoint = this.CreatePoint(position);
                newPoint.AddTo(this);
            }
            else
            {
                selectedPoint.RemoveFrom(this);
            }
        }

        private ConnectedComponentViewModel? EditingComponent { get; set; }
        private void Interactor_MouseDoubleClicked(object sender, Vector2 e)
        {
            EditingComponent = ConnectedComponents.FirstOrDefault(i => i.CollidesWith(CollisionType.Normal, e));
            //Get editable component
            //Listen to key presses

        }
        private void Interactor_MouseClicked(object sender, Vector2 e)
        {
            EditingComponent = null;
        }

        private void Interactor_KeyPressed(object sender, string e)
        {
            if(EditingComponent != null)
            {
                if(e.Length == 1)
                {
                    EditingComponent.Label += e;
                }else if(e == "Backspace")
                {
                    var newLength = EditingComponent.Label.Length - 1;
                    if(newLength >= 0)
                    {
                        EditingComponent.Label = EditingComponent.Label.Substring(0, newLength);
                    }
                }
                else if (e == "Enter")
                {
                    EditingComponent = null;
                }
            }
        }

        #endregion

        #region Selected Components
        public void ClearSelectedItems()
        {
            SelectedItems.ToList().ForEach(c =>c.Deselect(this));
            SelectedItems.Clear();
            SelectedItemChanged?.Invoke(this, SelectedItems);
        }

        public void SelectComponent(ConnectedComponentViewModel component)
        {
            SelectedItems.Add(component);
            component.Select(this);
            SelectedItemChanged?.Invoke(this, SelectedItems);
        }

        public void DeselectComponent(ConnectedComponentViewModel component)
        {
            if (!SelectedItems.Remove(component))
            {
                return;
            }

            component.Deselect(this);
            SelectedItemChanged?.Invoke(this, SelectedItems);
        }

        private ConnectionPointViewModel? _selectedConnectionPoint;

        public ConnectionPointViewModel? SelectedConnectionPoint
        {
            get => _selectedConnectionPoint;
            set
            {
                value?.Select(this);
                SetProperty(ref _selectedConnectionPoint, value);
                SelectedConnectionChanged?.Invoke(this, _selectedConnectionPoint);
            }
        }

        #endregion

        #region Mouse



        private void SelectionReleased()
        {
            Selection.IsShown = false;
            var selectedItems = ConnectedComponents.Where(i => i.CollidesWith(CollisionType.Normal, Selection.Points.ToArray()));
            selectedItems.ToList().ForEach(c => SelectComponent(c));
        }

        private void CreateSelectionReleased()
        {
            CreateSelection.IsShown = false;
            var collidingItems =
                ConnectedComponents.Where(i => i.CollidesWith(CollisionType.Normal, CreateSelection.Points.ToArray()));
            if (!collidingItems.Any())
            {
                //Create a new item
                this.CreateConnectedComponent(string.Empty, CreateSelection.Position, CreateSelection.Size);
            }
        }

        private void StartSelection(Vector2 position)
        {
            ClearSelectedItems();
            Selection.Show(position);
        }

        private void StartCreateSelection(Vector2 position)
        {
            ClearSelectedItems();
            CreateSelection.Show(position);
        }

        private void StartResizing(ConnectedComponentViewModel component)
        {
            ClearSelectedItems();
            SelectComponent(component);
        }

        private void StartMoving(ConnectedComponentViewModel component)
        {
            MovingItem = true;
            SelectComponent(component);
        }

        private void ClickComponent(ConnectedComponentViewModel component, Vector2 position)
        {
            //If the border of the item was selected -> resize the item
            ResizeDirection = component.GetCollidingEdge(position);
            if (ResizeDirection == ResizeDirection.None)
            {
                StartMoving(component);
            }
            //If the center of the item was selected -> Move the selected items
            else
            {
                StartResizing(component);
            }
        }

        private void MoveComponent(Vector2 position)
        {
            var delta = position - Interactor.LastMousePosition;
            var cancellationTokenSource = new CancellationTokenSource();
            var movingItems = SelectedItems.ToList();
            foreach (var item in SelectedItems)
            {
                this.Move(item, delta, movingItems, cancellationTokenSource);
            }

            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            foreach (var connection in Connections)
            {
                if (movingItems.Contains(connection.Point1.Item) && movingItems.Contains(connection.Point2.Item))
                {
                    connection.Path.Translate(delta);
                }
                else if (movingItems.Any(i =>
                        connection.Path.CollidesWith(CollisionType.Margin, i.Points.ToArray()) ||
                        i is ConnectedComponentViewModel connectedComponent &&
                        connection.IsConnectedTo(connectedComponent)))
                    //If the item is moving or is connected to a moving item

                {
                    connection.Redraw();
                }
            }
        }

        private void ResizeItem(Vector2 position)
        {
            var selectedItem = SelectedItems.FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }

            var delta = position - Interactor.LastMousePosition;
            selectedItem.Resize(this, delta, ResizeDirection);
        }

        private void ItemReleased()
        {
            MovingItem = false;
            ResizeDirection = ResizeDirection.None;

            if (Interactor.ControlPressed)
            {
                return;
            }

            Connections.ToList().ForEach(c => c.Deselect(this));
            ClearSelectedItems();
        }

        private void UpdateNewConnectionPath(Vector2 position)
        {
            if (_selectedConnectionPoint == null || ConnectedComponents.Any(i => i.CollidesWith(CollisionType.Normal, position)))
            {
                return;
            }

            NewConnectionPath.ClearPoints();
            var startPoint = _selectedConnectionPoint.GetOffset(_selectedConnectionPoint.Item.Margin);
            var path = this.GetPath(startPoint, position);

            path.Add(_selectedConnectionPoint.GetOffset(_selectedConnectionPoint.ConnectionPointSize));
            NewConnectionPath.AddPoints(path);
            MousePoint.Position = position;
            MousePoint.IsShown = true;
            NewConnectionPath.IsShown = true;
        }

        private void ReleaseNewConnectionPath(Vector2 position)
        {
            MousePoint.IsShown = false;
            NewConnectionPath.IsShown = false;
            NewConnectionPath.ClearPoints();

            var selectedConnectionPoint = GetComponents<ConnectionPointViewModel>()
                .FirstOrDefault(c => c.CollidesWith(CollisionType.Normal, position));

            if (selectedConnectionPoint == null)
            {
                var collidingItem = ConnectedComponents.FirstOrDefault(c => c.CollidesWith(CollisionType.Normal, position));
                if (collidingItem != null && collidingItem != _selectedConnectionPoint.Item)
                {
                    selectedConnectionPoint = this.CreateConnectionPoint(collidingItem, position);
                    selectedConnectionPoint.Update(_selectedConnectionPoint.AnchorPoint);
                }
            }

            if (SelectedConnectionPoint != selectedConnectionPoint && selectedConnectionPoint != null)
            {
                _selectedConnectionPoint.Connect(this, selectedConnectionPoint);
            }

            if (SelectedConnectionPoint?.Connections.Count == 0)
            {
                SelectedConnectionPoint.RemoveFrom(this);
            }
        }

        private void MoveConnection(Vector2 position)
        {
            SelectedConnectionPoint?.Update(position);

            if (Interactor.IsPressed("c"))
            {
                UpdateNewConnectionPath(position);
            }
        }

        private void ConnectionReleased(Vector2 position)
        {
            _selectedConnectionPoint.Deselect(this);
            _selectedConnectionPoint = null;
        }

        #endregion
    }
}