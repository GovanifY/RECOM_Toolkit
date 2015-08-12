#include <stddef.h>
#include <stdio.h>	

     
    typedef struct
    {
            unsigned char *ptrBufferCurrent;
            unsigned char lastFlag;
            unsigned char lastByte;
    } State;
    int decBlock(State* state, int flagToCheck)
    {
            int result = 0;
            for (flagToCheck--; flagToCheck >= 0; flagToCheck--)
            {
                    state->lastFlag >>= 1;
                    if (state->lastFlag == 0)
                    {
                            state->lastFlag = 128;
                            state->lastByte = *state->ptrBufferCurrent++;
                    }
                    result <<= 1;
                    if (state->lastByte & state->lastFlag)
                            result++;
            }
            state->lastFlag = state->lastFlag;
            state->lastByte = state->lastByte;
            return result;
    }
    int encBlock(State* state, int Flag, int Mask)
    {
            Flag = 1 << Flag;
            while (1)
            {
                    Flag >>= 1;
                    if (Flag == 0)
                            break;
                    if (Flag & Mask)
                            state->lastByte |= state->lastFlag;
                    state->lastFlag >>= 1;
                    if (state->lastFlag == 0)
                    {
                            *state->ptrBufferCurrent++ = state->lastByte;
                            state->lastByte = 0;
                            state->lastFlag = 128;
                    }
            }
            return state->lastFlag;
    }
    void finalizeBlock(State* state)
    {
            if (state->lastFlag != 128)
                    *state->ptrBufferCurrent++ = state->lastByte;
    }
     
    void *createMemory(size_t count, size_t size)
    {
            void *result = calloc(count, size);
            if (!result)
                    printf("Allocate memory error\n");
            return result;
    }
    void *resizeMemory(void *Memory, size_t count, size_t size)
    {
            void *result = realloc(Memory, count * size);
            if (!result)
                    printf("Reallocate memory error\n");
            return result;
    }
    void deleteMemory(void *Memory)
    {
            free(Memory);
    }
    int __cdecl getFileLength(const char *filename)
    {
            FILE *f;
            long length;
     
            f = fopen(filename, "rb");
            if (!f)
                    printf("File open error\n");
            length = _filelength(f->_file);
            if (fclose(f) == -1)
                    printf("File close error\n");
            return length;
    }
     
     
    unsigned char * Encode(unsigned char *SrcData)
    {
            const size_t BUFFER_BLOCK_SIZE = 0x100000;
     
            int v1;
            int v2;
            int v3;
            int v4;
            int v6;
            int v7;
            int v8;
            unsigned char v10;
            unsigned char *v11;
            unsigned char *v12;
            unsigned char *v13;
            unsigned char *v14;
     
            int DstDataMaxLength;
            unsigned int fileIndex;
            signed int equalFound;
     
            State state;
            int fileLength;
            unsigned char *DstData;
     
            DstData = createMemory(BUFFER_BLOCK_SIZE, 1);
            DstDataMaxLength = BUFFER_BLOCK_SIZE;
            fileIndex = 0;
            v1 = 0;
            state.ptrBufferCurrent = DstData;
            while (fileIndex < fileLength)
            {
                    int dicCurIndex;
                    int dicMaxIndex;
     
                    unsigned char dic[0x200];
                    unsigned char *PtrBuffer;
                    v2 = v1 - (state.ptrBufferCurrent - DstData);
                    for (v2--; v2 >= 0; v2--)
                            *state.ptrBufferCurrent++ = 0;
                    PtrBuffer = SrcData + fileIndex;
     
                    // Load data into dictionary
                    dic[0] = 0;
                    for (dicMaxIndex = 1; dicMaxIndex < 512; dicMaxIndex++)
                    {
                            if (PtrBuffer - SrcData == fileLength)
                                    break; // EOF
                            dic[dicMaxIndex] = *PtrBuffer++;
                    }
     
                    state.lastByte = 0;
                    state.lastFlag = 128;
                    dicCurIndex = 1;
                    do
                    {
                            unsigned char dataFound;
     
                            if (dicCurIndex >= dicMaxIndex)
                                    break;
     
                            // check if buffer reach its maximum size
                            while (1)
                            {
                                    unsigned int ptrDiff = state.ptrBufferCurrent - DstData;
                                    if ((DstDataMaxLength - (state.ptrBufferCurrent - DstData)) > 2)
                                            break;
                                    DstDataMaxLength += BUFFER_BLOCK_SIZE;
                                    DstData = resizeMemory(DstData, DstDataMaxLength, 1);
                                    state.ptrBufferCurrent = DstData + ptrDiff;
                            }
                            v7 = 1;
                            if (dicCurIndex > 0x100)
                                    v7 = dicCurIndex - 256;
                            v8 = dicMaxIndex - dicCurIndex;
                            if ((dicMaxIndex - dicCurIndex) > 0x11)
                                    v8 = 17;
                            v10 = 0;
                            equalFound = 1;
                            dataFound = dic[dicCurIndex];
                            v14 = &dic[dicCurIndex - 1];
                            for (v3 = dicCurIndex - 1; v3 >= v7; v3--)
                            {
                                    if ((v3 & 0xFF) && *v14 == dataFound)
                                    {
                                            int found;
                                            v11 = &dic[dicCurIndex];
                                            v12 = v14;
                                            for (found = 1; found < v8; found++)
                                            {
                                                    unsigned char b1 = *++v11;
                                                    unsigned char b2 = *++v12;
                                                    if (b1 != b2)
                                                            break;
                                            }
                                            if (found > equalFound)
                                            {
                                                    v10 = v3;
                                                    equalFound = found;
                                                    if (found == v8)
                                                            break;
                                            }
                                    }
                                    v14--;
                            }
                            if (equalFound <= 1)
                            {
                                    encBlock(&state, 1, 1);
                                    encBlock(&state, 8, dataFound);
                                    equalFound = 1;
                            }
                            else
                            {
                                    encBlock(&state, 1, 0);
                                    encBlock(&state, 8, v10);
                                    encBlock(&state, 4, equalFound - 2);
                            }
                            dicCurIndex += equalFound;
                            if (dicCurIndex > 0x1EE)
                            {
                                    int j = 0;
                                    dicCurIndex -= 256;
                                    do
                                    {
                                            dic[j++] = dic[j + 0x100];
                                    } while (j != 256);
                                    v13 = &dic[dicMaxIndex];
                                    for (dicMaxIndex -= 256; dicMaxIndex <= 0x1FF; dicMaxIndex++)
                                    {
                                            v4 = PtrBuffer;
                                            v13++;
                                            if (PtrBuffer - SrcData == fileLength)
                                                    break;
                                            v13[-257] = *PtrBuffer;
                                            PtrBuffer = v4 + 1;
                                    }
                            }
                            fileIndex += equalFound;
                    } while (state.ptrBufferCurrent - DstData <= (v1 + 4096 - 0x10));
                    encBlock(&state, 1, 0);
                    encBlock(&state, 8, 0);
                    finalizeBlock(&state);
                    v1 += 4096;
            }
            deleteMemory(SrcData);
            return DstData;
    }
    unsigned char * Decode(unsigned char *SrcData)
    {
            const size_t BUFFER_BLOCK_SIZE = 0x100000;
     
            size_t fileLength;
            unsigned char *DstData;
            int DstDataMaxLength;
            unsigned int remainingBlocks;
            unsigned char* pInData;
            unsigned char* PtrDst;
            State state;
     
            DstData = (unsigned char*)createMemory(BUFFER_BLOCK_SIZE, 1);
            DstDataMaxLength = 0x100000;
            PtrDst = DstData;
            pInData = SrcData;
     
            for (remainingBlocks = (fileLength + 4095) >> 12; remainingBlocks > 0; remainingBlocks--)
            {
                    int i;
                    int repeatCount = 0;
                    unsigned char dic[0x100] = { 0 };
     
                    state.ptrBufferCurrent = pInData;
                    state.lastFlag = 0;
                    state.lastByte = 0;
                    for (i = 1; ; i += repeatCount + 2)
                    {
                            unsigned char readByte;
                            int dicIndex;
                            int posStart;
                            int j;
     
                            while (1)
                            {
                                    // check if buffer reach its maximum size
                                    while (1)
                                    {
                                            unsigned int ptrDiff = PtrDst - DstData;
                                            if ((DstDataMaxLength - (PtrDst - DstData)) > 0x10)
                                                    break;
                                            DstDataMaxLength += BUFFER_BLOCK_SIZE;
                                            DstData = resizeMemory(DstData, DstDataMaxLength, 1);
                                            PtrDst = DstData + ptrDiff;
                                    }
                                    // check if there is uncompressed data
                                    if (decBlock(&state, 1) == 0)
                                            break;
                                    // read uncompressed data and store it into a dictionary
                                    readByte = decBlock(&state, 8);
                                    dic[i & 0xFF] = readByte;
                                    *PtrDst++ = readByte;
                                    i++;
                            }
                            // check if data should be read from dictionary
                            dicIndex = decBlock(&state, 8);
                            if (dicIndex == 0)
                                    break;
                            posStart = i;
                            repeatCount = decBlock(&state, 4);
                            for (j = repeatCount + 1; j >= 0; j--)
                            {
                                    unsigned int pos = posStart++;
                                    unsigned int index = dicIndex++;
                                    readByte = dic[index & 0xFF];
                                    dic[pos & 0xFF] = readByte;
                                    *PtrDst++ = readByte;
                            }
                    }
                    pInData += 0x1000;
            }
            deleteMemory(SrcData);
            return DstData;
    }

