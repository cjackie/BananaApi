#ifndef UTIL_H
#define UTIL_H

#include <vector>
#include <string>
#include <stdio.h>
#include <sstream>

std::string strprintf(const char *format, ...);

template <typename T>
std::string HexStr(const T itbegin, const T itend, bool fSpaces = true)
{
    const unsigned char *pbegin = (const unsigned char *)&itbegin[0];
    const unsigned char *pend = pbegin + (itend - itbegin) * sizeof(itbegin[0]);

    std::stringstream ss;
    for (const unsigned char *p = pbegin; p != pend; p++) {
        if ((int)(*p) < 16)
            ss << std::hex << "0" << (int)(*p);
        else
            ss << std::hex << (int)(*p);
        if (fSpaces && p != pend - 1)
            ss << " ";
    }
    return ss.str();
}

inline std::string HexStr(std::vector<unsigned char> vch, bool fSpaces = true)
{
    return HexStr(vch.begin(), vch.end(), fSpaces);
}

#endif // UTIL_H