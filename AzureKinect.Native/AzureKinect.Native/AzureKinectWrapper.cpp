#include "pch.h"
#include "AzureKinectWrapper.h"
#include "k4a/k4a.h"

std::shared_ptr<AzureKinectWrapper> AzureKinectWrapper::instance = nullptr;

AzureKinectWrapper::AzureKinectWrapper(ID3D11Device *device)
{
    InitializeCriticalSection(&resourcesCritSec);
    this->d3d11Device = device;
}

AzureKinectWrapper::~AzureKinectWrapper()
{
    DeleteCriticalSection(&resourcesCritSec);
	this->d3d11Device = nullptr;
	StopStreamingAll();
}

unsigned int AzureKinectWrapper::GetDeviceCount()
{
    return k4a_device_get_installed_count();
}

bool AzureKinectWrapper::TryGetDeviceSerialNumber(unsigned int index, char *serialNum, unsigned int serialNumSize)
{
    uint32_t device_count = k4a_device_get_installed_count();
    if (index > device_count - 1)
    {
        return false;
    }

    k4a_device_t device = NULL;
    bool closeDevice = false;
    if (deviceMap.count(index) > 0)
    {
        device = deviceMap[index];
    }
    else if (K4A_RESULT_SUCCEEDED == k4a_device_open(index, &device))
    {
        closeDevice = true;
    }
    else
    {
        k4a_device_close(device);
        return false;
    }

    size_t serialNumLength = 0;
    if (K4A_BUFFER_RESULT_TOO_SMALL != k4a_device_get_serialnum(device, serialNum, &serialNumLength) ||
        serialNumLength > serialNumSize)
    {
        return false;
    }

    if (closeDevice)
    {
        k4a_device_close(device);
    }
    device = NULL;
    return true;
}

bool AzureKinectWrapper::TryGetShaderResourceViews(
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
	unsigned int &pointCloudTemplateBpp)
{
    EnterCriticalSection(&resourcesCritSec);
    if (resourcesMap.count(index) == 0)
    {
        LeaveCriticalSection(&resourcesCritSec);
        OutputDebugString(L"Resources not created for device: " + index);
        return false;
    }

    rgbSrv = resourcesMap[index].rgbSrv;
    rgbWidth = resourcesMap[index].rgbFrameDimensions.width;
    rgbHeight = resourcesMap[index].rgbFrameDimensions.height;
    rgbBpp = resourcesMap[index].rgbFrameDimensions.bpp;

    depthSrv = resourcesMap[index].depthSrv;
    depthWidth = resourcesMap[index].depthFrameDimensions.width;
    depthHeight = resourcesMap[index].depthFrameDimensions.height;
    depthBpp = resourcesMap[index].depthFrameDimensions.bpp;

	pointCloudTemplateSrv = resourcesMap[index].pointCloudTemplateSrv;
	pointCloudTemplateWidth = resourcesMap[index].pointCloudTemplateFrameDimensions.width;
	pointCloudTemplateHeight = resourcesMap[index].pointCloudTemplateFrameDimensions.height;
	pointCloudTemplateBpp = resourcesMap[index].pointCloudTemplateFrameDimensions.bpp;

    LeaveCriticalSection(&resourcesCritSec);
    return true;
}

bool AzureKinectWrapper::TryStartStreams(
	unsigned int index,
	k4a_image_format_t colorFormat,
	k4a_color_resolution_t colorResolution,
	k4a_depth_mode_t depthMode,
	k4a_fps_t fps)
{
	if (deviceMap.count(index) > 0)
	{
		return true;
	}

	k4a_device_t k4aDevice = NULL;
	k4a_result_t result = K4A_RESULT_SUCCEEDED;
	k4a_calibration_t calibration;
	k4a_transformation_t transformation;
	k4a_image_t transformedColorImage;
	k4a_image_t xyTableImage;

	if (static_cast<unsigned int>(index) > GetDeviceCount() - 1)
	{
		OutputDebugString(L"Provided index did not exist: " + index);
		goto FailedExit;
	}

	if (K4A_RESULT_SUCCEEDED != k4a_device_open(index, &k4aDevice))
	{
		OutputDebugString(L"Failed to open device: " + index);
		goto FailedExit;
	}

	// Not all of the modes support 30fps, view k4a.c to determine a valid configuration
	config.color_format = colorFormat; // K4A_IMAGE_FORMAT_COLOR_BGRA32;
	config.color_resolution = colorResolution; // K4A_COLOR_RESOLUTION_2160P;
	config.depth_mode = depthMode; // K4A_DEPTH_MODE_WFOV_2X2BINNED;
	config.camera_fps = fps; // K4A_FRAMES_PER_SECOND_30;

	if (K4A_RESULT_SUCCEEDED != k4a_device_start_cameras(k4aDevice, &config))
	{
		OutputDebugString(L"Failed to start cameras: " + index);
		goto FailedExit;
	}

	deviceMap[index] = k4aDevice;

	k4a_device_get_calibration(k4aDevice, config.depth_mode, config.color_resolution, &calibration);
	calibrationMap[index] = calibration;
	transformation = k4a_transformation_create(&calibration);
	transformationMap[index] = transformation;

	k4a_image_create(K4A_IMAGE_FORMAT_COLOR_BGRA32,
		calibration.depth_camera_calibration.resolution_width,
		calibration.depth_camera_calibration.resolution_height,
		calibration.depth_camera_calibration.resolution_width * 4 * (int)sizeof(uint8_t),
		&transformedColorImage);
	transformedColorMap[index] = transformedColorImage;

	k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM,
		calibration.depth_camera_calibration.resolution_width,
		calibration.depth_camera_calibration.resolution_height,
		calibration.depth_camera_calibration.resolution_width * (int)sizeof(k4a_float2_t),
		&xyTableImage);
	create_xy_table(&calibration, xyTableImage);
	xyTableMap[index] = xyTableImage;

	cachedColorImageSizeMap[index] = calibration.color_camera_calibration.resolution_width * calibration.color_camera_calibration.resolution_height * 4 * sizeof(uint8_t);
	cachedColorImageBufferMap[index] = std::make_unique<byte[]>(cachedColorImageSizeMap[index]);

	return true;

FailedExit:
	if (k4aDevice != NULL)
	{
		k4a_device_close(k4aDevice);
	}
	return false;
}

bool AzureKinectWrapper::TryUpdate()
{
    if (deviceMap.size() == 0)
    {
        OutputDebugString(L"No devices created, update failed");
        return false;
    }

    k4a_capture_t capture = NULL;
    bool observedFailure = false;
	for (auto pair : deviceMap)
	{
		auto device = pair.second;

		switch (k4a_device_get_capture(device, &capture, 0))
		{
		case K4A_WAIT_RESULT_SUCCEEDED:
			break;
		case K4A_WAIT_RESULT_TIMEOUT:
			OutputDebugString(L"Timed out waiting for capture: " + pair.first);
			observedFailure = true;
			continue;
		case K4A_WAIT_RESULT_FAILED:
			OutputDebugString(L"Failed to capture: " + pair.first);
			observedFailure = true;
			continue;
		}

		EnterCriticalSection(&resourcesCritSec);
		if (resourcesMap.count(pair.first) == 0)
		{
			resourcesMap[pair.first] = DeviceResources{
				nullptr, nullptr, {}, nullptr, nullptr, {}, nullptr, nullptr, {}
			};
		}

		DeviceResources &resources = resourcesMap[pair.first];
		LeaveCriticalSection(&resourcesCritSec);

		auto transformation = transformationMap[pair.first];
		auto transformedColorImage = transformedColorMap[pair.first];

		auto colorImage = k4a_capture_get_color_image(capture);
		auto depthImage = k4a_capture_get_depth_image(capture);

		if (colorImage &&
			depthImage)
		{
			if (cachedColorImageBufferMap.count(pair.first) != 0)
			{
				auto colorImageBuffer = k4a_image_get_buffer(colorImage);
				memcpy(cachedColorImageBufferMap[pair.first].get(), colorImageBuffer, k4a_image_get_size(colorImage));
			}

			k4a_result_t result = k4a_transformation_color_image_to_depth_camera(
				transformation,
				depthImage,
				colorImage,
				transformedColorImage);

			if (result == K4A_RESULT_SUCCEEDED)
			{
				UpdateResources(transformedColorImage,
					resources.rgbSrv,
					resources.rgbTexture,
					resources.rgbFrameDimensions,
					DXGI_FORMAT_B8G8R8A8_UNORM);
			}
		}

		if (depthImage)
		{
			UpdateResources(depthImage,
				resources.depthSrv,
				resources.depthTexture,
				resources.depthFrameDimensions,
				DXGI_FORMAT_R16_UNORM);
		}

		if (depthImage &&
			pointCloudTemplateMap.count(pair.first) == 0)
		{
			auto xyTableImage = xyTableMap[pair.first];
			auto calibration = calibrationMap[pair.first];

			k4a_image_t pointCloudTemplateImage;
			k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM,
				calibration.depth_camera_calibration.resolution_width,
				calibration.depth_camera_calibration.resolution_height,
				calibration.depth_camera_calibration.resolution_width * (int)sizeof(k4a_float3_t),
				&pointCloudTemplateImage);
			pointCloudTemplateMap[pair.first] = pointCloudTemplateImage;

			k4a_image_t depthTemplateImage;
			k4a_image_create(k4a_image_get_format(depthImage),
				calibration.depth_camera_calibration.resolution_width,
				calibration.depth_camera_calibration.resolution_height,
				k4a_image_get_stride_bytes(depthImage),
				&depthTemplateImage);

			// Getting depth projection for 1m depth;
			auto buffer = k4a_image_get_buffer(depthTemplateImage);
			for (int i = 0; i < k4a_image_get_size(depthTemplateImage); i += 2)
			{
				*reinterpret_cast<uint16_t*>(&(buffer[i])) = 1000;
			}

			int pointCount = 0;
			generate_point_cloud(depthTemplateImage,
				xyTableImage,
				pointCloudTemplateImage,
				&pointCount);
			UpdateResources(pointCloudTemplateImage,
				resources.pointCloudTemplateSrv,
				resources.pointCloudTemplateTexture,
				resources.pointCloudTemplateFrameDimensions,
				DXGI_FORMAT_R32G32B32_FLOAT);

			k4a_image_release(depthTemplateImage);
		}

		k4a_image_release(colorImage);
		k4a_image_release(depthImage);
		k4a_capture_release(capture);
	}

    return !observedFailure;
}

bool AzureKinectWrapper::TryGetCalibration(
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
	float *depthIntrinsics)
{
	if (calibrationMap.count(index) == 0)
	{
		return false;
	}

	auto calibration = calibrationMap.at(index);
	
	*colorWidth = calibration.color_camera_calibration.resolution_width;
	*colorHeight = calibration.color_camera_calibration.resolution_height;
	memcpy(colorRotation, calibration.color_camera_calibration.extrinsics.rotation, sizeof(float) * 9);
	memcpy(depthTranslation, calibration.color_camera_calibration.extrinsics.translation, sizeof(float) * 3);
	*colorIntrinsicsCount = calibration.color_camera_calibration.intrinsics.parameter_count;
	if ((*colorIntrinsicsCount) > colorIntrinsicsLength)
	{
		return false;
	}
	memcpy(colorIntrinsics, calibration.color_camera_calibration.intrinsics.parameters.v, sizeof(float) * (*colorIntrinsicsCount));

	*depthWidth = calibration.depth_camera_calibration.resolution_width;
	*depthHeight = calibration.depth_camera_calibration.resolution_height;
	memcpy(depthRotation, calibration.depth_camera_calibration.extrinsics.rotation, sizeof(float) * 9);
	memcpy(depthTranslation, calibration.depth_camera_calibration.extrinsics.translation, sizeof(float) * 3);
	*depthIntrinsicsCount = calibration.depth_camera_calibration.intrinsics.parameter_count;
	if ((*depthIntrinsicsCount) > depthIntrinsicsLength)
	{
		return false;
	}
	memcpy(depthIntrinsics, calibration.depth_camera_calibration.intrinsics.parameters.v, sizeof(float) * (*depthIntrinsicsCount));

	return true;
}

bool AzureKinectWrapper::TryGetCachedColorImage(int index, byte *data, int size, int *imageWidth, int *imageHeight, int *bytesPerPixel)
{
	if (cachedColorImageSizeMap.count(index) == 0 ||
		cachedColorImageBufferMap.count(index) == 0 ||
		cachedColorImageSizeMap[index] > size ||
		calibrationMap.count(index) == 0)
	{
		return false;
	}

	memcpy(data, cachedColorImageBufferMap[index].get(), cachedColorImageSizeMap[index]);
	*imageWidth = calibrationMap[index].color_camera_calibration.resolution_width;
	*imageHeight = calibrationMap[index].color_camera_calibration.resolution_height;
	*bytesPerPixel = 4; // Assuming BGRA8
	return true;
}

void AzureKinectWrapper::StopStreamingAll()
{
	std::vector<int> indices;
	for(auto device : deviceMap)
	{
		indices.push_back(device.first);
	}

	for (auto index : indices)
	{
		StopStreaming(index);
	}
}

void AzureKinectWrapper::StopStreaming(unsigned int index)
{
    if (deviceMap.count(index) > 0)
    {
        OutputDebugString(L"Closed device: " + index);
        k4a_device_close(deviceMap[index]);
		deviceMap.erase(index);
    }
    else
    {
        OutputDebugString(L"Asked to close unknown device: " + index);
    }

	if (calibrationMap.count(index) != 0)
	{
		calibrationMap.erase(index);
	}

	if (transformationMap.count(index) != 0)
	{
		k4a_transformation_destroy(transformationMap[index]);
		transformationMap.erase(index);
	}

	if (transformedColorMap.count(index) != 0)
	{
		k4a_image_release(transformedColorMap[index]);
		transformedColorMap.erase(index);
	}

	if (xyTableMap.count(index) != 0)
	{
		k4a_image_release(xyTableMap[index]);
		xyTableMap.erase(index);
	}

	if (pointCloudTemplateMap.count(index) != 0)
	{
		k4a_image_release(pointCloudTemplateMap[index]);
		pointCloudTemplateMap.erase(index);
	}

	if (cachedColorImageBufferMap.count(index) != 0)
	{
		cachedColorImageBufferMap.erase(index);
	}
}

void AzureKinectWrapper::UpdateResources(k4a_image_t image,
                                         ID3D11ShaderResourceView *&srv,
                                         ID3D11Texture2D *&tex,
                                         FrameDimensions &dim,
                                         DXGI_FORMAT format)
{
    EnterCriticalSection(&resourcesCritSec);
    dim.height = k4a_image_get_height_pixels(image);
    dim.width = k4a_image_get_width_pixels(image);
    auto stride = k4a_image_get_stride_bytes(image);
    dim.bpp = stride / dim.width;
    auto buffer = k4a_image_get_buffer(image);

    if (tex == nullptr)
    {
        tex = DirectXHelper::CreateTexture(d3d11Device, buffer, dim.width, dim.height, dim.bpp, format);
    }

    if (srv == nullptr)
    {
        srv = DirectXHelper::CreateShaderResourceView(d3d11Device, tex, format);
    }
    else
    {
        OutputDebugString(L"Updating shader resource view");
        DirectXHelper::UpdateShaderResourceView(d3d11Device, srv, buffer, stride);
    }

    LeaveCriticalSection(&resourcesCritSec);
}