using System;
using Cairo;
using Vintagestory.API.Client;

namespace XInvTweaksFork;

/// <summary>A small padlock badge; one texture per client session, independent of slot count.</summary>
internal sealed class SlotLockOverlay : IDisposable
{
    private readonly ICoreClientAPI api;
    private int textureId;

    public SlotLockOverlay(ICoreClientAPI api)
    {
        this.api = api;
    }

    public void Render(ElementBounds bounds)
    {
        if (textureId == 0)
        {
            using var surface = new ImageSurface(Format.Argb32, 32, 32);
            using var context = new Context(surface);
            // A subdued outline on transparency, leaving the item underneath visible.
            context.SetSourceRGBA(0, 0, 0, 0.8);
            context.LineWidth = 2.5;
            context.Arc(16, 12, 7, Math.PI, 2 * Math.PI);
            context.LineTo(23, 18);
            context.MoveTo(9, 12);
            context.LineTo(9, 18);
            context.Stroke();
            context.Rectangle(5, 15, 22, 14);
            context.Stroke();
            context.Rectangle(14.5, 19, 3, 6);
            context.Fill();
            GuiElement.GenerateTexture(api, surface, ref textureId);
        }

        var size = bounds.OuterWidth * 0.32;
        api.Render.Render2DTexturePremultipliedAlpha(textureId,
            bounds.renderX + bounds.OuterWidth - size - 2, bounds.renderY + 2, size, size, 300);
    }

    public void Dispose()
    {
        if (textureId != 0) api.Gui.DeleteTexture(textureId);
        textureId = 0;
    }
}
