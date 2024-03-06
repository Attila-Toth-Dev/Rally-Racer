using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hierarchy2
{
    public static class UIElementsExtensions
    {
        public static void StyleDisplay(this VisualElement _ui, DisplayStyle _displayStyle) =>
            _ui.style.display = _displayStyle;

        public static void StyleDisplay(this VisualElement _ui, bool _value) =>
            _ui.StyleDisplay(_value ? DisplayStyle.Flex : DisplayStyle.None);

        public static bool IsDisplaying(this VisualElement _ui) => _ui.style.display == DisplayStyle.Flex;

        public static void StyleVisibility(this VisualElement _ui, Visibility _visibility) =>
            _ui.style.visibility = _visibility;

        public static void StyleVisibility(this VisualElement _ui, bool _value) =>
            _ui.StyleVisibility(_value ? Visibility.Visible : Visibility.Hidden);

        public static Vector2 StylePosition(this VisualElement _ui) => 
            new(_ui.style.left.value.value, _ui.style.top.value.value);

        public static Vector2 StyleSize(this VisualElement _ui) => 
            new(_ui.style.width.value.value, _ui.style.height.value.value);

        public static Vector2 StyleMinSize(this VisualElement _ui) => 
            new(_ui.style.minWidth.value.value, _ui.style.minHeight.value.value);

        public static Vector2 StyleMaxSize(this VisualElement _ui) => 
            new(_ui.style.maxWidth.value.value, _ui.style.maxHeight.value.value);

        public static void StylePosition(this VisualElement _ui, Vector2 _position)
        {
            _ui.StyleLeft(_position.x);
            _ui.StyleTop(_position.y);
        }

        public static void StylePosition(this VisualElement _ui, StyleLength _x, StyleLength _y)
        {
            _ui.StyleLeft(_x);
            _ui.StyleTop(_y);
        }

        public static void StyleTop(this VisualElement _ui, StyleLength _value) => _ui.style.top = _value;

        public static void StyleBottom(this VisualElement _ui, StyleLength _value) => _ui.style.bottom = _value;

        public static void StyleLeft(this VisualElement _ui, StyleLength _value) => _ui.style.left = _value;

        public static void StyleRight(this VisualElement _ui, StyleLength _value) => _ui.style.right = _value;

        public static float StyleTop(this VisualElement _ui) => _ui.style.top.value.value;

        public static float StyleBottom(this VisualElement _ui) => _ui.style.bottom.value.value;

        public static float StyleLeft(this VisualElement _ui) => _ui.style.left.value.value;

        public static float StyleRight(this VisualElement _ui) => _ui.style.right.value.value;

        public static void StylePosition(this VisualElement _ui, Position _type) => _ui.style.position = _type;

        public static void StyleSize(this VisualElement _ui, StyleLength _width, StyleLength _height)
        {
            _ui.StyleWidth(_width);
            _ui.StyleHeight(_height);
        }

        public static void StyleSize(this VisualElement _ui, Vector2 _size) => StyleSize(_ui, _size.x, _size.y);

        public static void StyleMinSize(this VisualElement _ui, StyleLength _width, StyleLength _height)
        {
            _ui.StyleMinWidth(_width);
            _ui.StyleMinHeight(_height);
        }

        public static void StyleMaxSize(this VisualElement _ui, StyleLength _width, StyleLength _height)
        {
            _ui.StyleMaxWidth(_width);
            _ui.StyleMaxHeight(_height);
        }


        public static void StyleWidth(this VisualElement _ui, StyleLength _width) => _ui.style.width = _width;

        public static void StyleMinWidth(this VisualElement _ui, StyleLength _width) => _ui.style.minWidth = _width;

        public static void StyleMaxWidth(this VisualElement _ui, StyleLength _width) => _ui.style.maxWidth = _width;

        public static void StyleHeight(this VisualElement _ui, StyleLength _height) => _ui.style.height = _height;

        public static void StyleMinHeight(this VisualElement _ui, StyleLength _height) => _ui.style.minHeight = _height;

        public static void StyleMaxHeight(this VisualElement _ui, StyleLength _height) => _ui.style.maxHeight = _height;

        public static void StyleFont(this VisualElement _ui, FontStyle _fontStyle) =>
            _ui.style.unityFontStyleAndWeight = _fontStyle;

        public static void StyleFontSize(this VisualElement _ui, StyleLength _size) => _ui.style.fontSize = _size;

        public static void StyleTextAlign(this VisualElement _ui, TextAnchor _textAnchor) =>
            _ui.style.unityTextAlign = _textAnchor;

        public static void StyleAlignSelf(this VisualElement _ui, Align _align) => _ui.style.alignSelf = _align;

        public static void StyleAlignItem(this VisualElement _ui, Align _align) => _ui.style.alignItems = _align;

        public static void StyleJustifyContent(this VisualElement _ui, Justify _justify) =>
            _ui.style.justifyContent = _justify;

        public static void StyleFlexDirection(this VisualElement _ui, FlexDirection _flexDirection) =>
            _ui.style.flexDirection = _flexDirection;

        public static void StyleMargin(this VisualElement _ui, StyleLength _value) =>
            _ui.StyleMargin(_value, _value, _value, _value);

        public static void StyleMargin(this VisualElement _ui, StyleLength _left, StyleLength _right, StyleLength _top,
            StyleLength _bottom)
        {
            _ui.style.marginLeft = _left;
            _ui.style.marginRight = _right;
            _ui.style.marginTop = _top;
            _ui.style.marginBottom = _bottom;
        }

        public static void StyleMarginLeft(this VisualElement _ui, StyleLength _value) => _ui.style.marginLeft = _value;

        public static void StyleMarginRight(this VisualElement _ui, StyleLength _value) => _ui.style.marginRight = _value;

        public static void StyleMarginTop(this VisualElement _ui, StyleLength _value) => _ui.style.marginTop = _value;

        public static void StyleMarginBottom(this VisualElement _ui, StyleLength _value) => _ui.style.marginBottom = _value;

        public static void StylePadding(this VisualElement _ui, StyleLength _value) =>
            _ui.StylePadding(_value, _value, _value, _value);

        public static void StylePadding(this VisualElement _ui, StyleLength _left, StyleLength _right, StyleLength _top,
            StyleLength _bottom)
        {
            _ui.style.paddingLeft = _left;
            _ui.style.paddingRight = _right;
            _ui.style.paddingTop = _top;
            _ui.style.paddingBottom = _bottom;
        }

        public static void StylePaddingLeft(this VisualElement _ui, StyleLength _value) => _ui.style.paddingLeft = _value;

        public static void StylePaddingRight(this VisualElement _ui, StyleLength _value) => _ui.style.paddingRight = _value;

        public static void StylePaddingTop(this VisualElement _ui, StyleLength _value) => _ui.style.paddingTop = _value;

        public static void StylePaddingBottom(this VisualElement _ui, StyleLength _value) =>
            _ui.style.paddingBottom = _value;

        public static void StyleBorderRadius(this VisualElement _ui, StyleLength _radius) =>
            _ui.StyleBorderRadius(_radius, _radius, _radius, _radius);

        public static void StyleBorderRadius(this VisualElement _ui, StyleLength _topLeft, StyleLength _topRight,
            StyleLength _bottomLeft, StyleLength _bottomRight)
        {
            _ui.style.borderTopLeftRadius = _topLeft;
            _ui.style.borderTopRightRadius = _topRight;
            _ui.style.borderBottomLeftRadius = _bottomLeft;
            _ui.style.borderBottomRightRadius = _bottomRight;
        }

        public static void StyleBorderWidth(this VisualElement _ui, StyleFloat _width) =>
            _ui.StyleBorderWidth(_width, _width, _width, _width);

        public static void StyleBorderWidth(this VisualElement _ui, StyleFloat _left, StyleFloat _right, StyleFloat _top,
            StyleFloat _bottom)
        {
            _ui.style.borderLeftWidth = _left;
            _ui.style.borderRightWidth = _right;
            _ui.style.borderTopWidth = _top;
            _ui.style.borderBottomWidth = _bottom;
        }

        public static void StyleBorderColor(this VisualElement _ui, StyleColor _color) =>
            _ui.StyleBorderColor(_color, _color, _color, _color);

        public static void StyleBorderColor(this VisualElement _ui, StyleColor _left, StyleColor _right, StyleColor _top,
            StyleColor _bottom)
        {
            _ui.style.borderLeftColor = _left;
            _ui.style.borderRightColor = _right;
            _ui.style.borderTopColor = _top;
            _ui.style.borderBottomColor = _bottom;
        }

        public static void StyleFlexBasisAsPercent(this VisualElement _ui, StyleLength _basis) =>
            _ui.style.flexBasis = _basis;

        public static void StyleFlexGrow(this VisualElement _ui, StyleFloat _grow) => _ui.style.flexGrow = _grow;

        public static void StyleBackgroundColor(this VisualElement _ui, StyleColor _color) =>
            _ui.style.backgroundColor = _color;

        public static void StyleTextColor(this VisualElement _ui, StyleColor _color) => _ui.style.color = _color;

        public static VisualElement FindChildren(this VisualElement _ui, string _name) => 
            _ui.Children().ToList().Find(_childElement => _childElement.name == _name);

        public static T FindChildren<T>(this VisualElement _ui, string _name) where T : VisualElement 
            => _ui.FindChildren(_name) as T;

        public static VisualElement FindChildrenPhysicalHierarchy(this VisualElement _ui, string _name) => 
            _ui.hierarchy.Children().ToList().Find(_child => _child.name == _name);

        public static VisualElement FindChildrenPhysicalHierarchy<T>(this VisualElement _ui, string _name)
            where T : VisualElement
        {
            return _ui.FindChildrenPhysicalHierarchy(_name) as T;
        }
    }
}