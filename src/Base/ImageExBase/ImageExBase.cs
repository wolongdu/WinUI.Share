﻿// Copyright (c) Richasy. All rights reserved.

using System;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Web.Http;

namespace Richasy.WinUI.Share.Base;

/// <summary>
/// 图片扩展基类.
/// </summary>
public abstract partial class ImageExBase : LayoutControlBase
{
    private const int MaxRetryCount = 3;
    private static readonly HttpClient _httpClient = new();
    private Uri? _lastUri;

    // private int _retryCount;
    private ImageBrush? _backgroundBrush;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageExBase"/> class.
    /// </summary>
    protected ImageExBase()
    {
        DefaultStyleKey = typeof(ImageExBase);
    }

    /// <summary>
    /// 图片已加载.
    /// </summary>
    public event EventHandler ImageLoaded;

    /// <summary>
    /// 图片加载失败.
    /// </summary>
    public event EventHandler ImageFailed;

    /// <summary>
    /// 获取中心裁切区域.
    /// </summary>
    /// <param name="targetRect">预计渲染区域.</param>
    /// <param name="sourceRect">图片原始区域.</param>
    /// <returns><see cref="Rect"/>.</returns>
    /// <remarks>
    /// 这里会根据预期渲染的大小和图片的宽高比计算出图片在控件中的渲染区域.
    /// </remarks>
    protected static Rect GetCenterCropRect(Rect targetRect, Rect sourceRect)
    {
        var targetAspectRatio = targetRect.Width / targetRect.Height;
        var sourceAspectRatio = sourceRect.Width / sourceRect.Height;

        double scaleFactor;
        double scaledWidth, scaledHeight;

        if (targetRect.Width - sourceRect.Width > 0.1 && targetRect.Height - sourceRect.Height > 0.1)
        {
            // targetRect is larger than sourceRect in both dimensions
            scaleFactor = Math.Max(targetRect.Width / sourceRect.Width, targetRect.Height / sourceRect.Height);
            scaledWidth = sourceRect.Width * scaleFactor;
            scaledHeight = sourceRect.Height * scaleFactor;

            // Ensure the scaled size does not exceed target size
            if (scaledWidth > targetRect.Width)
            {
                scaleFactor = targetRect.Width / sourceRect.Width;
                scaledWidth = sourceRect.Width * scaleFactor;
                scaledHeight = sourceRect.Height * scaleFactor;
            }

            if (scaledHeight > targetRect.Height)
            {
                scaleFactor = targetRect.Height / sourceRect.Height;
                scaledWidth = sourceRect.Width * scaleFactor;
                scaledHeight = sourceRect.Height * scaleFactor;
            }
        }
        else
        {
            // targetRect is smaller or similar in size to sourceRect
            if (targetAspectRatio > sourceAspectRatio)
            {
                // Target is wider than source
                scaleFactor = sourceRect.Width / targetRect.Width;
                scaledWidth = sourceRect.Width;
                scaledHeight = targetRect.Height * scaleFactor;
            }
            else
            {
                // Target is taller than source
                scaleFactor = sourceRect.Height / targetRect.Height;
                scaledWidth = targetRect.Width * scaleFactor;
                scaledHeight = sourceRect.Height;
            }
        }

        var offsetX = (sourceRect.Width - scaledWidth) / 2;
        var offsetY = (sourceRect.Height - scaledHeight) / 2;

        return new Rect(sourceRect.X + offsetX, sourceRect.Y + offsetY, scaledWidth, scaledHeight);
    }

    /// <summary>
    /// 绘制图片.
    /// </summary>
    protected abstract void DrawImage(CanvasBitmap canvasBitmap);

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        var rootBorder = GetTemplateChild("Root") as Panel ?? throw new InvalidOperationException("TemplateRoot not found.");
        if (rootBorder.Background is ImageBrush brush)
        {
            _backgroundBrush = brush;
        }
        else
        {
            _backgroundBrush = new ImageBrush() { Stretch = Stretch.UniformToFill };
            rootBorder.Background = _backgroundBrush;
        }
    }

    /// <inheritdoc/>
    protected override async void OnControlLoaded()
    {
        // 理论上这里要考虑 DeviceLost 的情况，但是这里暂时不处理。
        if (_backgroundBrush?.ImageSource is null)
        {
            await RedrawAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        CanvasImageSource = default;
        if (_backgroundBrush is not null)
        {
            _backgroundBrush.ImageSource = default;
            _backgroundBrush = null;
        }
    }
}
