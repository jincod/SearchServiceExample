using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Nest;
using Nest.Resolvers;

namespace SearchServices
{
    public class SearchService
    {
        private const string IndexType = "indexName";
        private readonly ElasticClient _client;

        public SearchService()
        {
            var node = new Uri("http://localhost:9200/");
            var settings = new ConnectionSettings(node, IndexType);
            _client = new ElasticClient(settings);
        }

        public void Reindex()
        {
            _client.DeleteIndex(IndexType);
            _client.CreateIndex(IndexType);

            var indexDefinition = new RootObjectMapping
                {
                    Properties = new Dictionary<PropertyNameMarker, IElasticType>()
                };

            var paramsProperty = new ObjectMapping
                {
                    Properties = new Dictionary<PropertyNameMarker, IElasticType>()
                };

            var numberMapping = new NumberMapping();
            var boolMapping = new BooleanMapping();
            var stringMapping = new StringMapping
                {
                    Index = FieldIndexOption.NotAnalyzed
                };

            paramsProperty.Properties.Add("productPropertyId1", boolMapping);
            paramsProperty.Properties.Add("productPropertyId2", numberMapping);
            paramsProperty.Properties.Add("productPropertyId3", boolMapping);
            // ...
            paramsProperty.Properties.Add("productPropertyIdn", stringMapping);

            indexDefinition.Properties.Add(Property.Name<ProductIndex>(p => p.Params), paramsProperty);
            indexDefinition.Properties.Add(Property.Name<ProductIndex>(p => p.Id), stringMapping);
            indexDefinition.Properties.Add(Property.Name<ProductIndex>(p => p.CategoryId), stringMapping);

            _client.Map<ProductIndex>(x => x.InitializeUsing(indexDefinition));

            IEnumerable<Product> products = GetAllProducts();

            _client.IndexMany(products);
        }

        private static IEnumerable<Product> GetAllProducts()
        {
            return new List<Product>();
        }

        public IEnumerable<string> SearchCategories(IEnumerable<object> filters)
        {
            FilterContainer filterContainer = BuildParamsFilter(filters);

            var request = new SearchRequest
                {
                    Size = 0,
                    Aggregations = new Dictionary<string, IAggregationContainer>
                        {
                            {
                                "agg", new AggregationContainer
                                    {
                                        Filter = new FilterAggregator
                                            {
                                                Filter = filterContainer
                                            },
                                        Aggregations = new Dictionary<string, IAggregationContainer>
                                            {
                                                {
                                                    "categoryId", new AggregationContainer
                                                        {
                                                            Terms = new TermsAggregator
                                                                {
                                                                    Size = 0,
                                                                    Field =
                                                                        Property.Path<ProductIndex>(p => p.CategoryId)
                                                                }
                                                        }
                                                }
                                            }
                                    }
                            }
                        }
                };

            ISearchResponse<ProductIndex> result = _client.Search<ProductIndex>(request);

            SingleBucket filterAgg = result.Aggs.Filter("agg");
            if (filterAgg != null)
            {
                IEnumerable<string> categoryIds =
                    filterAgg.Terms("categoryId").Items
                        .Select(item => item.Key)
                        .ToList();
                return categoryIds;
            }
            return Enumerable.Empty<string>();
        }

        private FilterContainer BuildParamsFilter(IEnumerable<object> filters)
        {
            // build NEST Filter object from filters
            return new FilterContainer();
        }

        public IEnumerable<string> SearchProducts(IEnumerable<object> filters, string categoryId)
        {
            FilterContainer filterContainer =
                new FilterDescriptor<ProductIndex>().Term(t => t.CategoryId, categoryId) &&
                BuildParamsFilter(filters);

            var searchRequest = new SearchRequest
                {
                    Size = 0,
                    Aggregations = new Dictionary<string, IAggregationContainer>
                        {
                            {
                                "agg", new AggregationContainer
                                    {
                                        Filter = new FilterAggregator
                                            {
                                                Filter = filterContainer
                                            },
                                        Aggregations = new Dictionary<string, IAggregationContainer>
                                            {
                                                {
                                                    "productId", new AggregationContainer
                                                        {
                                                            Terms = new TermsAggregator
                                                                {
                                                                    Size = 0,
                                                                    Field = Property.Path<ProductIndex>(p => p.Id)
                                                                }
                                                        }
                                                }
                                            }
                                    }
                            }
                        }
                };

            ISearchResponse<ProductIndex> result = _client.Search<ProductIndex>(searchRequest);

            SingleBucket filterAgg = result.Aggs.Filter("agg");
            if (filterAgg != null)
            {
                IEnumerable<string> productIds =
                    filterAgg.Terms("productId").Items
                        .Select(item => item.Key)
                        .ToList();
                return productIds;
            }
            return Enumerable.Empty<string>();
        }
    }
}