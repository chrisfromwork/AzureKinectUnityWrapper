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
        ID3D11ShaderResourceView *&depthSrv,
        unsigned int &depthWidth,
        unsigned int &depthHeight,
        unsigned int &depthBpp,
		ID3D11ShaderResourceView *&pointCloudTemplateSrv,
		unsigned int &pointCloudTemplateWidth,
		unsigned int &pointCloudTemplateHeight,
		unsigned int &pointCloudTemplateBpp);
    bool TryStartStreams(
		unsigned int index,
		k4a_image_format_t colorFormat,
		k4a_color_resolution_t colorResolution,
		k4a_depth_mode_t depthMode,
		k4a_fps_t fps);
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
	bool TryGetImageBuffers(
		int index,
		byte *transformedColorImageData,
		int transformedColorImageSize,
		byte *depthImageData,
		int depthImageSize,
		byte *pointCloudTemplateImageData,
		int pointCloudTemplateImageSize);
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
        ID3D11Texture2D *depthTexture;
        ID3D11ShaderResourceView *depthSrv;
        FrameDimensions depthFrameDimensions;
		ID3D11Texture2D *pointCloudTemplateTexture;
		ID3D11ShaderResourceView *pointCloudTemplateSrv;
		FrameDimensions pointCloudTemplateFrameDimensions;
    };

	class ImageBuffer
	{
	public:
		ImageBuffer(FrameDimensions dimensions)
		{
			this->dimensions = dimensions;
			buffer = std::make_shared<std::vector<byte>>(GetSize());
		}

		int GetSize()
		{
			return dimensions.bpp * dimensions.height * dimensions.width;
		}

		FrameDimensions dimensions;
		std::shared_ptr<std::vector<byte>> buffer;
	};

    void UpdateResources(
		k4a_image_t image,
        ID3D11ShaderResourceView *&srv,
        ID3D11Texture2D *&tex,
        FrameDimensions &dim,
        DXGI_FORMAT format);
	void StopStreamingAll();

    ID3D11Device *d3d11Device;
    static std::shared_ptr<AzureKinectWrapper> instance;
    std::map<int, k4a_device_t> deviceMap;

	std::map<int, DeviceResources> resourcesMap;
    CRITICAL_SECTION resourcesCritSec;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	
	std::map<int, std::shared_ptr<ImageBuffer>> cachedTransformedColorImageBufferMap;
	std::map<int, std::shared_ptr<ImageBuffer>> cachedDepthImageBufferMap;
	std::map<int, std::shared_ptr<ImageBuffer>> cachedPointCloudTemplateImageBufferMap;
	std::map<int, k4a_calibration_t> calibrationMap;
	std::map<int, k4a_transformation_t> transformationMap;
	std::map<int, k4a_image_t> transformedColorMap;
	std::map<int, k4a_image_t> xyTableMap;
	std::map<int, k4a_image_t> pointCloudTemplateMap;
};
