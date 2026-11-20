import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    catalog_constant_load: {
      executor: 'constant-arrival-rate',
      rate: 100, // 100 requests per second
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 20,
      maxVUs: 100,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'], // 95% of requests must complete below 300ms (NFR-01)
    http_req_failed: ['rate<0.01'],    // Error rate must be less than 1%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5080';

const endpoints = [
  '/api/v1/products',
  '/api/v1/products?page=1&pageSize=20',
  '/api/v1/products?categorySlug=sepatu-lari',
  '/api/v1/products?q=pegasus',
  '/api/v1/products/nike-air-zoom-pegasus-40',
  '/api/v1/products/adidas-ultraboost-light',
  '/api/v1/products?sort=price_asc&minPrice=100000&maxPrice=1000000',
  '/api/v1/products/specs-lightspeed-4-pro-futsal'
];

export default function () {
  const url = `${BASE_URL}${endpoints[Math.floor(Math.random() * endpoints.length)]}`;
  const res = http.get(url, {
    tags: { name: 'CatalogEndpoint' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 300ms': (r) => r.timings.duration < 300,
  });
}
