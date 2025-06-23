using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class UXMLImage : Image
{
    #region Constructors

    public UXMLImage() : base()
    {

    }

    #endregion

    #region Properties

    [UxmlAttribute]
    public new Texture image
    {
        get => base.image;
        set => base.image = value;
    }

    [UxmlAttribute]
    public new Sprite sprite
    {
        get => base.sprite;
        set => base.sprite = value;
    }

    #endregion
}