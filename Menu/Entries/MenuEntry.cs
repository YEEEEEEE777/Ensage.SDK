﻿// <copyright file="MenuEntry.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace Ensage.SDK.Menu.Entries
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Ensage.SDK.Menu.Views;
    using Ensage.SDK.Renderer;

    using PlaySharp.Toolkit.Helper.Annotations;

    using SharpDX;

    public class MenuEntry : MenuBase
    {
        private readonly List<MenuBase> children = new List<MenuBase>();

        private bool isCollapsed = true;

        private bool isVisible = true;

        public MenuEntry(string name, IView view, IRenderer renderer, StyleRepository styleRepository, object instance, [CanBeNull] PropertyInfo propertyInfo)
            : base(name, view, renderer, styleRepository, instance, propertyInfo)
        {
        }

        public IReadOnlyCollection<MenuBase> Children
        {
            get
            {
                return this.children.AsReadOnly();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the menu entry is collapsed and if its children are visible.
        /// </summary>
        public bool IsCollapsed
        {
            get
            {
                return this.isCollapsed;
            }

            set
            {
                this.isCollapsed = value;
                foreach (var menuEntry in this.children.OfType<MenuEntry>())
                {
                    menuEntry.IsVisible = !this.isCollapsed;
                }
            }
        }

        public bool IsVisible
        {
            get
            {
                return this.isVisible;
            }

            private set
            {
                this.isVisible = value;
                foreach (var menuEntry in this.children.OfType<MenuEntry>())
                {
                    menuEntry.IsVisible = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the total size of this menu entry, including the size of all children.
        /// </summary>
        public Vector2 TotalSize { get; set; }

        public void AddChild(MenuBase child)
        {
            this.children.Add(child);
        }

        public override void Draw()
        {
            this.View.Draw(this);
        }

        public bool IsInsideMenu(Vector2 position)
        {
            return this.Position.X <= position.X
                   && this.Position.Y <= position.Y
                   && position.X <= (this.Position.X + this.TotalSize.X)
                   && position.Y <= (this.Position.Y + this.TotalSize.Y);
        }

        public override void OnClick(Vector2 clickPosition)
        {
            View.OnClick(this, clickPosition);
        }

        public void RemoveChild(MenuBase child)
        {
            this.children.Remove(child);
        }
    }
}