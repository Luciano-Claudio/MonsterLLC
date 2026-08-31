///////////////////////////////////////////////////////////////////////////////
// 
// This code is licensed under MIT license.
// 
// Copyright © 2014 Bil Simser, https://weblogs.aspnet/bsimser <bsimser@shaw.ca>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
///////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

/// <summary>
/// Simply re-styles a gameObject name in the Hierarchy window to be black and all caps.
/// Allows us to separate our gameObjects and not lose our minds.
/// </summary>
/// <example>
/// 1. Save this code to a file in a folder named "Editor" in Unity
/// 2. Create an empty game object
/// 3. Rename it with "//" as the prefix
/// 4. The object will be repainted in the hierarchy in white letters with a black background
/// </example>
/// <remarks>
/// Found in sample code on GitHub
/// </remarks>
[InitializeOnLoad]
public static class HierarchySectionHeader
{
    static HierarchySectionHeader()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
    }

    static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var gameObject = EditorUtility.EntityIdToObject(instanceID) as GameObject;

        if (gameObject != null && gameObject.name.StartsWith("//", System.StringComparison.Ordinal))
        {
            EditorGUI.DrawRect(selectionRect, Color.black);
            EditorGUI.DropShadowLabel(selectionRect, gameObject.name.Replace("/", "").ToUpperInvariant());
        }
    }
}