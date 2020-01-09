#pragma once

static void create_xy_table(const k4a_calibration_t *calibration, k4a_image_t xy_table)
{
	k4a_float2_t *table_data = (k4a_float2_t *)(void *)k4a_image_get_buffer(xy_table);

	int width = calibration->depth_camera_calibration.resolution_width;
	int height = calibration->depth_camera_calibration.resolution_height;

	k4a_float2_t p;
	k4a_float3_t ray;
	int valid;

	for (int y = 0, idx = 0; y < height; y++)
	{
		p.xy.y = (float)y;
		for (int x = 0; x < width; x++, idx++)
		{
			p.xy.x = (float)x;

			k4a_calibration_2d_to_3d(
				calibration, &p, 1.f, K4A_CALIBRATION_TYPE_DEPTH, K4A_CALIBRATION_TYPE_DEPTH, &ray, &valid);

			if (valid)
			{
				table_data[idx].xy.x = ray.xyz.x;
				table_data[idx].xy.y = ray.xyz.y;
			}
			else
			{
				table_data[idx].xy.x = nanf("");
				table_data[idx].xy.y = nanf("");
			}
		}
	}
}

static void generate_point_cloud(const k4a_image_t depth_image,
	const k4a_image_t xy_table,
	k4a_image_t point_cloud,
	int *point_count)
{
	int width = k4a_image_get_width_pixels(depth_image);
	int height = k4a_image_get_height_pixels(depth_image);

	uint16_t *depth_data = (uint16_t *)(void *)k4a_image_get_buffer(depth_image);
	k4a_float2_t *xy_table_data = (k4a_float2_t *)(void *)k4a_image_get_buffer(xy_table);
	k4a_float3_t *point_cloud_data = (k4a_float3_t *)(void *)k4a_image_get_buffer(point_cloud);

	*point_count = 0;
	for (int i = 0; i < width * height; i++)
	{
		if (depth_data[i] != 0 && !isnan(xy_table_data[i].xy.x) && !isnan(xy_table_data[i].xy.y))
		{
			point_cloud_data[i].xyz.x = xy_table_data[i].xy.x * (float)depth_data[i];
			point_cloud_data[i].xyz.y = xy_table_data[i].xy.y * (float)depth_data[i];
			point_cloud_data[i].xyz.z = (float)depth_data[i];
			(*point_count)++;
		}
		else
		{
			point_cloud_data[i].xyz.x = nanf("");
			point_cloud_data[i].xyz.y = nanf("");
			point_cloud_data[i].xyz.z = nanf("");
		}
	}
}