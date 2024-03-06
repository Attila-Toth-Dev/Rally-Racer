using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Hierarchy2
{
#if UNITY_EDITOR
	public class Foldout : VisualElement
	{
		public Image imageElement;
		public HorizontalLayout headerElement;
		public Label labelElement;
		public VerticalLayout contentElement;

		private Image foldoutImage;

		private readonly Texture onIcon = EditorGUIUtility.IconContent("IN foldout on@2x").image;
		private readonly Texture offIcon = EditorGUIUtility.IconContent("IN foldout@2x").image;

		private bool value;

		public bool Value
		{
			get => value;

			set
			{
				this.value = value;
				contentElement.StyleDisplay(this.value);
				foldoutImage.image = this.value ? onIcon : offIcon;
			}
		}

		public string Title
		{
			get => labelElement.text;
			// ReSharper disable once UnusedMember.Global
			set => labelElement.text = value;
		}

		public Foldout() => Init("");

		public Foldout(string _title) => Init(_title);

		private void Init(string _title)
		{
			this.StyleFont(FontStyle.Normal);
			this.StyleMinHeight(20);
			this.StyleBorderWidth(0, 0, 1, 0);
			Color borderColor = EditorGUIUtility.isProSkin
				                    ? new Color32(35, 35, 35, 255)
				                    : new Color32(153, 153, 153, 255);
			this.StyleBorderColor(borderColor);

			headerElement = new HorizontalLayout();
			headerElement.StyleHeight(21);
			headerElement.StyleMaxHeight(21);
			headerElement.StyleMinHeight(21);
			headerElement.StylePadding(4, 0, 0, 0);
			headerElement.StyleAlignItem(Align.Center);
			Color backgroundColor = EditorGUIUtility.isProSkin
				                        ? new Color32(80, 80, 80, 255)
				                        : new Color32(222, 222, 222, 255);
			headerElement.StyleBackgroundColor(backgroundColor);
			Color hoverBorderColor = new Color32(58, 121, 187, 255);
			headerElement.RegisterCallback<MouseEnterEvent>(_ =>
			{
				headerElement.StyleBorderWidth(1);
				headerElement.StyleBorderColor(hoverBorderColor);
			});
			headerElement.RegisterCallback<MouseLeaveEvent>(_ =>
			{
				headerElement.StyleBorderWidth(0);
				headerElement.StyleBorderColor(Color.clear);
			});
			base.Add(headerElement);

			contentElement = new VerticalLayout();
			contentElement.StyleDisplay(value);
			base.Add(contentElement);

			labelElement = new Label();
			labelElement.text = _title;
			headerElement.Add(labelElement);

			imageElement = new Image();
			imageElement.name = nameof(imageElement);
			imageElement.StyleMargin(0, 4, 0, 0);
			imageElement.StyleSize(16, 16);
			headerElement.Add(imageElement);
			imageElement.SendToBack();
			imageElement.RegisterCallback<GeometryChangedEvent>(_ => { imageElement.StyleDisplay(imageElement.image == null ? DisplayStyle.None : DisplayStyle.Flex); });

			foldoutImage = new Image();
			foldoutImage.StyleWidth(13);
			foldoutImage.StyleMargin(0, 2, 0, 0);
			foldoutImage.scaleMode = ScaleMode.ScaleToFit;
			foldoutImage.image = value ? onIcon : offIcon;
			if(!EditorGUIUtility.isProSkin)
				foldoutImage.tintColor = Color.grey;
			headerElement.Add(foldoutImage);
			foldoutImage.SendToBack();

			headerElement.RegisterCallback<MouseUpEvent>(_evt =>
			{
				if(_evt.button == 0)
				{
					Value = !Value;
					_evt.StopPropagation();
				}
			});
		}

		public new void Add(VisualElement _visualElement)
		{
			contentElement.Add(_visualElement);
		}
	}

	public class EditorHelpBox : VisualElement
	{
		public string Label { get; }

		public EditorHelpBox(string _text, MessageType _messageType, bool _wide = true)
		{
			style.marginLeft = style.marginRight = style.marginTop = style.marginBottom = 4;
			Label = _text;

			IMGUIContainer iMGUIContainer = new(() => { EditorGUILayout.HelpBox(Label, _messageType, _wide); });

			iMGUIContainer.name = nameof(IMGUIContainer);
			Add(iMGUIContainer);
		}
	}

#endif

	public class HorizontalLayout : VisualElement
	{
		public HorizontalLayout()
		{
			name = nameof(HorizontalLayout);
			this.StyleFlexDirection(FlexDirection.Row);
			this.StyleFlexGrow(1);
		}
	}

	public class VerticalLayout : VisualElement
	{
		public VerticalLayout()
		{
			name = nameof(VerticalLayout);
			this.StyleFlexDirection(FlexDirection.Column);
			this.StyleFlexGrow(1);
		}
	}
}