#pragma once
#include <string>
#include <iomanip>
#include <sstream>

namespace Utils
{
	const static char* ScryerLogTag = "[Scryer] ";
	
	inline std::string PadLeadingZeros(const int32_t input, const int32_t zeroCount)
	{
		std::stringstream ss;
		ss << std::setw(zeroCount) << std::setfill('0') << input;
		return ss.str();
	}
}
