#include "pch.h"
#include "AzureKinectFrame.h"


AzureKinectFrame::AzureKinectFrame(
	FrameDimensions colorImageDimensions,
	FrameDimensions depthImageDimensions,
	FrameDimensions pointCloudImageDimensions)
{
	_imageDimensions[static_cast<int>(AzureKinectImageType::Color)] = colorImageDimensions;
	_images[static_cast<int>(AzureKinectImageType::Color)] = new uint8_t[colorImageDimensions.bpp * colorImageDimensions.width * colorImageDimensions.height];
	_imageDimensions[static_cast<int>(AzureKinectImageType::Depth)] = depthImageDimensions;
	_images[static_cast<int>(AzureKinectImageType::Depth)] = new uint8_t[depthImageDimensions.bpp * depthImageDimensions.width * depthImageDimensions.height];
	_imageDimensions[static_cast<int>(AzureKinectImageType::PointCloud)] = pointCloudImageDimensions;
	_images[static_cast<int>(AzureKinectImageType::PointCloud)] = new uint8_t[pointCloudImageDimensions.bpp * pointCloudImageDimensions.width * pointCloudImageDimensions.height];
	_status = FrameStatus::Ready;
}

AzureKinectFrame::~AzureKinectFrame()
{
	for (int i = 0; i < AZURE_KINECT_IMAGE_TYPE_COUNT; i++)
	{
		delete[] _images[i];
	}
}

bool AzureKinectFrame::TryBeginWriting()
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Ready)
	{
		_status = FrameStatus::Writing;
		return true;
	}

	return false;
}

void AzureKinectFrame::WriteImage(
	AzureKinectImageType imageType,
	k4a_image_t image)
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Writing)
	{
		auto imageSize = k4a_image_get_stride_bytes(image) * k4a_image_get_height_pixels(image);
		auto dimensions = _imageDimensions[static_cast<int>(imageType)];
		auto localImageSize = dimensions.width * dimensions.height * dimensions.bpp;

		if (imageSize == localImageSize)
		{
			auto imageBuffer = k4a_image_get_buffer(image);
			memcpy(_images[static_cast<int>(imageType)], imageBuffer, localImageSize);
		}
	}
}

void AzureKinectFrame::EndWriting()
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Writing)
	{
		_status = FrameStatus::Ready;
	}
}

bool AzureKinectFrame::TryBeginReading()
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Ready)
	{
		_status = FrameStatus::Reading;
		return true;
	}

	return false;
}

byte* AzureKinectFrame::GetFrameBuffer(
	AzureKinectImageType imageType)
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Reading)
	{
		return _images[static_cast<int>(imageType)];
	}

	return nullptr;
}

void AzureKinectFrame::ReadImage(
	AzureKinectImageType imageType,
	ID3D11Device *d3d11Device,
	ID3D11ShaderResourceView *&srv,
	ID3D11Texture2D *&tex,
	FrameDimensions &dim)
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Reading)
	{
		auto dimensions = _imageDimensions[static_cast<int>(imageType)];
		dim.height = dimensions.height;
		dim.width = dimensions.width;
		dim.bpp = dimensions.bpp;
		auto stride = dim.bpp * dim.width;
		auto buffer = _images[static_cast<int>(imageType)];

		if (tex == nullptr ||
			srv == nullptr)
		{
			return;
		}

		DirectXHelper::UpdateShaderResourceView(d3d11Device, srv, buffer, stride);
	}
}

void AzureKinectFrame::EndReading()
{
	std::lock_guard<std::mutex> guard(_statusGuard);
	if (_status == FrameStatus::Reading)
	{
		_status = FrameStatus::Ready;
	}
}