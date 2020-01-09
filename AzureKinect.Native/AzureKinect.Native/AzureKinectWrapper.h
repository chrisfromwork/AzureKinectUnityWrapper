#pragma once

class AzureKinectWrapper
{
public:
    static unsigned int GetDeviceCount();

    AzureKinectWrapper(ID3D11Device *device);
    ~AzureKinectWrapper();
    bool TryGetDeviceSerialNumber(
		unsigned int index,
		char *serialNum,
		unsigned int serialNumSize);
    bool TryGetShaderResourceViews(
		unsigned int index,
        ID3D11ShaderResourceView *&rgbSrv,
        unsigned int &rgbWidth,
        unsigned int &rgbHeight,
        unsigned int &rgbBpp,
        ID3D11ShaderResourceView *&irSrv,
        unsigned int &irWidth,
        unsigned int &irHeight,
        unsigned int &irBpp,
        ID3D11ShaderResourceView *&depthSrv,
        unsigned int &depthWidth,
        unsigned int &depthHeight,
        unsigned int &depthBpp);
    bool TryStartStreams(unsigned int index);
    bool TryUpdate();
	bool TryGetCalibration(
		int index,
		int *colorWidth,
		int *colorHeight,
		float *colorRotation,
		float *colorTranslation,
		int *colorIntrinsicsCount,
		int colorIntrinsicsLength,
		float *colorIntrinsics,
		int *depthWidth,
		int *depthHeight,
		float *depthRotation,
		float *depthTranslation,
		int *depthIntrinsicsCount,
		int depthIntrinsicsLength,
		float *depthIntrinsics);
	bool TryGetCachedColorImage(
		int index,
		byte *data,
		int size,
		int *imageWidth,
		int *imageHeight,
		int *bytesPerPixel);
    void StopStreaming(unsigned int index);

private:
    struct FrameDimensions
    {
        unsigned int width;
        unsigned int height;
        unsigned int bpp;
    };

    struct DeviceResources
    {
        ID3D11Texture2D *rgbTexture;
        ID3D11ShaderResourceView *rgbSrv;
        FrameDimensions rgbFrameDimensions;
        ID3D11Texture2D *irTexture;
        ID3D11ShaderResourceView *irSrv;
        FrameDimensions irFrameDimensions;
        ID3D11Texture2D *depthTexture;
        ID3D11ShaderResourceView *depthSrv;
        FrameDimensions depthFrameDimensions;
    };

    void UpdateResources(k4a_image_t image,
                         ID3D11ShaderResourceView *&srv,
                         ID3D11Texture2D *&tex,
                         FrameDimensions &dim,
                         DXGI_FORMAT format);
	void StopStreamingAll();

    ID3D11Device *d3d11Device;
	k4a_device_t k4aDevice;
    static std::shared_ptr<AzureKinectWrapper> instance;
    std::map<int, k4a_device_t> deviceMap;

	std::map<int, DeviceResources> resourcesMap;
    CRITICAL_SECTION resourcesCritSec;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	
	std::map<int, int> cachedColorImageSizeMap;
	std::map<int, std::unique_ptr<byte[]>> cachedColorImageBufferMap;
	std::map<int, k4a_calibration_t> calibrationMap;
	std::map<int, k4a_transformation_t> transformationMap;
	std::map<int, k4a_image_t> transformedColorMap;
	std::map<int, k4a_image_t> xyTableMap;
	std::map<int, k4a_image_t> pointCloudMap;
};
