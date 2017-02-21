using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using RedBlack.Library.DataContracts;

namespace RedBlack.Library.DataAccess
{
    public class S3GameRepository : IGameRepository
    {
        public bool SaveGame(Game gameData)
        {
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                Key = $"{gameData.playerId}.json",
                ContentBody = JsonConvert.SerializeObject(gameData)
            };

            using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
            {
                PutObjectResponse response = null;

                Task.Run(async () =>
                {
                    response = await s3Client.PutObjectAsync(putObjectRequest);
                }).Wait();

                return response.HttpStatusCode == HttpStatusCode.OK;
            }
        }

        public Game FindGame(string playerId)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("playerS3BucketName"),
                Key = $"{playerId}.json"
            };

            using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.APSoutheast2))
            {
                GetObjectResponse response = null;

                Task.Run(async () =>
                {
                    response = await s3Client.GetObjectAsync(getObjectRequest);
                }).Wait();

                using (var streamReader = new StreamReader(response.ResponseStream))
                {
                    var content = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<Game>(content);
                }
            }
        }
    }
}