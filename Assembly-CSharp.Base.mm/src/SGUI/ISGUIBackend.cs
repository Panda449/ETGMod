﻿using System;
using UnityEngine;

namespace SGUI {
    public interface ISGUIBackend {

        /// <summary>
        /// The root that currently uses the backend to render.
        /// </summary>
        /// <value>The current root using the backend. Null when not rendering.</value>
        SGUIRoot CurrentRoot { get; }

        bool UpdateStyleOnRender { get; }

        /// <summary>
        /// Whether to use OnGUI (IMGUI) or not (GameObject-based / shadow hierarchy).
        /// </summary>
        /// <value><c>true</c> if the children of CurrentRoot should be rendered on and Start-/EndRender should get called in OnGUI.</value>
        bool RenderOnGUI { get; }

        float LineHeight { get; set; }

        void StartRender(SGUIRoot root);

        /// <summary>
        /// Render the specified text on screen.
        /// </summary>
        /// <param name="elem">Element instance. Null for root.</param>
        /// <param name="position">Relative position to render the text at.</param>
        /// <param name="text">Text to render.</param>
        void Text(SGUIElement elem, Vector2 position, string text);

        /// <summary>
        /// Render a text field on screen.
        /// </summary>
        /// <param name="elem">Element instance. Null for root.</param>
        /// <param name="position">Position.</param>
        /// <param name="text">Text.</param>
        void TextField(SGUIElement elem, Vector2 position, ref string text);

        /// <summary>
        /// Gets the size of the text.
        /// </summary>
        /// <returns>The size of the given text.</returns>
        /// <param name="text">The text to measure.</param>
        /// <param name="size">The bounds in which the text should fit in.</param>
        Vector2 MeasureText(string text, Vector2? size = null);

        void EndRender(SGUIRoot root);

        // Text auto-generated by MonoDevelop. Nice! -- 0x0ade
        /// <summary>
        /// Releases all resource used by the <see cref="T:WTFGUI.IWTFGUIBackend"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:WTFGUI.IWTFGUIBackend"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:WTFGUI.IWTFGUIBackend"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="T:WTFGUI.IWTFGUIBackend"/>
        /// so the garbage collector can reclaim the memory that the <see cref="T:WTFGUI.IWTFGUIBackend"/> was occupying.</remarks>
        void Dispose();

    }
}
