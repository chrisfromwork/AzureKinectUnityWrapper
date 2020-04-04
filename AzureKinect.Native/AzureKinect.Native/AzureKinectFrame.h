#pragma once

#include "FrameDimensions.h"
#include <mutex>

#define AZURE_KINECT_IMAGE_TYPE_COUNT 3

enum class AzureKinectImageType : int
{
	Color = 0,
	Depth = 1,
	PointCloud = 2
};

class AzureKinectFrame
{
public:
	AzureKinectFrame(
		FrameDimensions colorImageDimensions,
		FrameDimensions depthImageDimensions,
		FrameDimensions pointCloudImageDimensions);
	~AzureKinectFrame();

	bool TryBeginWriting();
	void WriteImage(
		AzureKinectImageType imageType,
		k4a_image_t image);
	void EndWriting();
	bool TryBeginReading();
	byte* GetFrameBuffer(
		AzureKinectImageType imageType);
	void ReadImage(
		AzureKinectImageType imageType,
		ID3D11Device *d3d11Device,
		ID3D11ShaderResourceView *&srv,
		ID3D11Texture2D *&tex,
		FrameDimensions &dim);
	void EndReading();
	
private:
	enum class FrameStatus
	{
		Ready,
		Writing,
		Reading
	};

	FrameDimensions _imageDimensions[AZURE_KINECT_IMAGE_TYPE_COUNT];
	byte* _images[AZURE_KINECT_IMAGE_TYPE_COUNT] = { nullptr };
	std::mutex _statusGuard;
	FrameStatus _status;
};

