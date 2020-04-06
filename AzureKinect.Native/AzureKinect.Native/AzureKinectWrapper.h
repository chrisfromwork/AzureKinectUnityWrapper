#pragma once

#include "FrameDimensions.h"
#include "AzureKinectFrame.h"
#include <atomic>

#define MAX_NUM_CACHED_FRAMES 5

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
	bool TryGetCachedColorImage(
		int index,
		byte *data,
		int size,
		int *imageWidth,
		int *imageHeight,
		int *bytesPerPixel);
    void StopStreaming(unsigned int index);

private:
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

	void StopStreamingAll();
	void RunCaptureLoop(int index);
	void CreateResources(
		ID3D11ShaderResourceView *&srv,
		ID3D11Texture2D *&tex,
		FrameDimensions &dim,
		DXGI_FORMAT format);

    ID3D11Device *d3d11Device;
    static std::shared_ptr<AzureKinectWrapper> instance;
    std::map<int, k4a_device_t> deviceMap;

	std::map<int, std::shared_ptr<DeviceResources>> resourcesMap;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	
	std::map<int, int> cachedColorImageSizeMap;
	std::map<int, std::unique_ptr<byte[]>> cachedColorImageBufferMap;
	std::map<int, k4a_calibration_t> calibrationMap;
	std::map<int, k4a_transformation_t> transformationMap;
	std::map<int, k4a_image_t> transformedColorMap;
	std::map<int, k4a_image_t> xyTableMap;
	std::map<int, k4a_image_t> pointCloudTemplateMap;

	std::map<int, std::shared_ptr<AzureKinectFrame>> _frameMap;
	std::map<int, std::shared_ptr<std::thread>> _captureThreads;
	std::map<int, std::atomic<bool>> _stopRequestedMap;
};
