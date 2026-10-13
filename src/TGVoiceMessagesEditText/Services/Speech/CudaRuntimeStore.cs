using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TGVoiceMessagesEditText.Configuration;

namespace TGVoiceMessagesEditText.Services.Speech;

public class CudaRuntimeStore(PortablePaths paths, ILogger<CudaRuntimeStore> logger)
{
    private const string BaseUrl = "https://developer.download.nvidia.com/compute/cuda/redist/";

    private static readonly string[] LoadOrder = ["cudart64_12.dll", "cublasLt64_12.dll", "cublas64_12.dll"];

    private static readonly CudaArchive[] Archives =
    [
        new(
            "cuda_cudart/windows-x86_64/cuda_cudart-windows-x86_64-12.9.79-archive.zip",
            "179e9c43b0735ffe67207b3da556eb5a0c50f3047961882b7657d3b822d34ef8",
            ["cudart64_12.dll"]),
        new(
            "libcublas/windows-x86_64/libcublas-windows-x86_64-12.9.2.10-archive.zip",
            "62782c354e1536118caab8fe5fd295ac5fd9076e4814aba0bdaf032fe2b833ae",
            ["cublasLt64_12.dll", "cublas64_12.dll"])
    ];

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        foreach (var archive in Archives)
        {
            if (archive.Libraries.Any(library => !File.Exists(LibraryPath(library))))
            {
                await DownloadAsync(archive, cancellationToken);
            }
        }

        foreach (var library in LoadOrder)
        {
            NativeLibrary.Load(LibraryPath(library));
        }
    }

    private string LibraryPath(string library) => Path.Combine(paths.CudaDirectory, library);

    private async Task DownloadAsync(CudaArchive archive, CancellationToken cancellationToken)
    {
        if (!paths.TryEnsureWritableDirectory(paths.CudaDirectory))
        {
            throw new InvalidOperationException($"CUDA libraries are missing and {paths.CudaDirectory} is read-only.");
        }

        var archivePath = Path.Combine(paths.CudaDirectory, Path.GetFileName(archive.RelativeUrl) + ".partial");
        try
        {
            logger.LogInformation("Downloading CUDA runtime archive {Archive}", Path.GetFileName(archive.RelativeUrl));
            using (var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
            using (var response = await http.GetAsync(BaseUrl + archive.RelativeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await DownloadProgress.CopyAsync(source, target, logger, cancellationToken);
            }

            await VerifyChecksumAsync(archivePath, archive.Sha256, cancellationToken);
            ExtractLibraries(archivePath, archive.Libraries);
        }
        finally
        {
            File.Delete(archivePath);
        }
    }

    private static async Task VerifyChecksumAsync(string archivePath, string expectedSha256, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(archivePath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Checksum mismatch for {Path.GetFileName(archivePath)}: expected {expectedSha256}, got {actual}.");
        }
    }

    private void ExtractLibraries(string archivePath, string[] libraries)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        foreach (var library in libraries)
        {
            var entry = zip.Entries.FirstOrDefault(candidate => candidate.Name.Equals(library, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"{library} is not found in {Path.GetFileName(archivePath)}.");
            var partialPath = LibraryPath(library) + ".partial";
            entry.ExtractToFile(partialPath, true);
            File.Move(partialPath, LibraryPath(library), true);
        }
    }
}
