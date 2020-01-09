#pragma once
#include <d3d11_1.h>

class DirectXHelper
{
public:
	static ID3D11ShaderResourceView* CreateShaderResourceView(
		ID3D11Device* device,
		ID3D11Texture2D* texture,
		DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
    {
        ID3D11ShaderResourceView *srv;
        D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
        D3D11_TEXTURE2D_DESC texDesc;

        texture->GetDesc(&texDesc);
        srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
        srvDesc.Texture2D.MostDetailedMip = 0;
        srvDesc.Texture2D.MipLevels = texDesc.MipLevels;
        srvDesc.Format = format;

        device->CreateShaderResourceView(texture, &srvDesc, &srv);
		return srv;
	}

	static void UpdateShaderResourceView(ID3D11Device *device, ID3D11ShaderResourceView *srv, const byte *bytes, int stride)
    {
        ID3D11Texture2D *tex = NULL;
        srv->GetResource((ID3D11Resource **)(&tex));

        if (tex == NULL)
        {
            return;
        }

        ID3D11DeviceContext *ctx = NULL;
        device->GetImmediateContext(&ctx);

        if (ctx == NULL)
        {
            return;
        }

        ctx->UpdateSubresource(tex, 0, NULL, bytes, stride, 0);
        ctx->Release();
    }

    static ID3D11Texture2D* CreateTexture(
		ID3D11Device* device,
        const byte* bytes,
        int width,
        int height,
        int bpp,
        DXGI_FORMAT textureFormat = DXGI_FORMAT_R8G8B8A8_UNORM)
    {
        if (device == nullptr)
        {
            return nullptr;
        }

        ID3D11Texture2D *tex;

        D3D11_TEXTURE2D_DESC tdesc;
        D3D11_SUBRESOURCE_DATA tbsd;

        // setting up D3D11_SUBRESOURCE_DATA
        tbsd.pSysMem = (void *)bytes;
        tbsd.SysMemPitch = width * bpp;

        // setting up D3D11_TEXTURE2D_DESC
        tdesc.Width = width;
        tdesc.Height = height;
        tdesc.MipLevels = 1;
        tdesc.ArraySize = 1;
        tdesc.SampleDesc.Count = 1;
        tdesc.SampleDesc.Quality = 0;
        tdesc.Usage = D3D11_USAGE_DEFAULT;
        tdesc.Format = textureFormat;
        tdesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        tdesc.CPUAccessFlags = 0;
        tdesc.MiscFlags = 0;

        device->CreateTexture2D(&tdesc, &tbsd, &tex);
        return tex;
    }
};