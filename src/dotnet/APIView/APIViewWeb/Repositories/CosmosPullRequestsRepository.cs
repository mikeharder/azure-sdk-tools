// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIViewWeb.Models;
using APIViewWeb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace APIViewWeb
{
    public class CosmosPullRequestsRepository : ICosmosPullRequestsRepository
    {
        private readonly Container _pullRequestsContainer;
        private ICosmosReviewRepository _reviewsRepository;

        public CosmosPullRequestsRepository(IConfiguration configuration, ICosmosReviewRepository reviewsRepository, CosmosClient cosmosClient)
        {
            _pullRequestsContainer = cosmosClient.GetContainer(configuration["CosmosDBName"], "PullRequests");
            _reviewsRepository = reviewsRepository;
        }

        public async Task<PullRequestModel> GetPullRequestAsync(int pullRequestNumber, string repoName, string packageName, string language = null)
        {
            var queryBuilder  =  new StringBuilder($"SELECT * FROM PullRequests c WHERE c.PullRequestNumber = {pullRequestNumber} AND c.RepoName = '{repoName}' AND c.PackageName = '{packageName}' AND c.IsDeleted = false");
            if (language != null)
            {
                queryBuilder.Append($" AND IS_DEFINED(c.Language) AND c.Language = '{language}'");
            }
            var requests = await GetPullRequestFromQueryAsync(queryBuilder.ToString());
            return requests.Count > 0 ? requests[0] : null;
        }

        public async Task UpsertPullRequestAsync(PullRequestModel pullRequestModel)
        {
            await _pullRequestsContainer.UpsertItemAsync(pullRequestModel, new PartitionKey(pullRequestModel.ReviewId));
        }

        public async Task<IEnumerable<PullRequestModel>> GetPullRequestsAsync(bool isOpen)
        {
            var query = $"SELECT * FROM PullRequests c WHERE c.IsOpen = {(isOpen? "true": "false")} AND c.IsDeleted = false";
            return await GetPullRequestFromQueryAsync(query);
        }

        public async Task<List<PullRequestModel>> GetPullRequestsAsync(int pullRequestNumber, string repoName)
        {
            var query = $"SELECT * FROM PullRequests c WHERE c.PullRequestNumber = {pullRequestNumber} and c.RepoName = '{repoName}' AND c.IsDeleted = false";
            return await GetPullRequestFromQueryAsync(query);
        }

        public async Task<IEnumerable<PullRequestModel>> GetPullRequestsAsync(string reviewId, string apiRevisionId = null) {
            var query = $"SELECT * FROM PullRequests c WHERE c.ReviewId = '{reviewId}' AND c.IsDeleted = false";
            if (!string.IsNullOrEmpty(apiRevisionId))
            {
                query += $" AND c.APIRevisionId = '{apiRevisionId}'";
            }

            return await GetPullRequestFromQueryAsync(query);
        }

        private async Task<List<PullRequestModel>> GetPullRequestFromQueryAsync(string query)
        {
            var allRequests = new List<PullRequestModel>();
            var itemQueryIterator = _pullRequestsContainer.GetItemQueryIterator<PullRequestModel>(query);
            while (itemQueryIterator.HasMoreResults)
            {
                var result = await itemQueryIterator.ReadNextAsync();
                allRequests.AddRange(result.Resource);
            }

            // Cosmos doesn't allow cross join of two containers so we need to filter closed API reviews
            var filtered = new List<PullRequestModel>();
            Dictionary<string, List<PullRequestModel>> kvp = new Dictionary<string, List<PullRequestModel>>();
            foreach (var pr in allRequests)
            {
                if (!string.IsNullOrEmpty(pr.ReviewId))
                {
                    if (kvp.ContainsKey(pr.ReviewId))
                    {
                        kvp[pr.ReviewId].Add(pr);
                    }
                    else
                    {
                        kvp.Add(pr.ReviewId, new List<PullRequestModel> { pr });    
                    }
                }
            }

            if (kvp.Any())
            {
                var reviews = await _reviewsRepository.GetReviewsAsync(reviewIds: new List<string>(kvp.Keys), isClosed: false);
                var reviewIds = reviews.Select(r => r.Id).ToList();

                foreach (var kv in kvp)
                {
                    if (reviewIds.Contains(kv.Key))
                    {
                        filtered.AddRange(kv.Value);
                    }
                }
            }
            return filtered;
        }
    }
}
