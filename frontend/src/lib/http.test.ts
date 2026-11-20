import { describe, expect, it, beforeEach, vi } from 'vitest';
import { AxiosError, InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import { http, resetRefreshQueueForTesting } from './http';
import { useAuthStore } from '@/stores/authStore';

describe('HTTP Axios Interceptor & Single Queue Refresh', () => {
  beforeEach(() => {
    resetRefreshQueueForTesting();
    useAuthStore.setState({
      user: null,
      token: 'old-expired-token',
      isAuthenticated: true,
      isLoading: false,
    });
  });

  it('multiple concurrent 401 responses trigger only a single refresh request and resolve all calls with new token', async () => {
    let refreshCallCount = 0;
    const requestAttempts: Record<string, number> = {
      '/cart': 0,
      '/me': 0,
      '/orders': 0,
    };

    http.defaults.adapter = async (config: InternalAxiosRequestConfig): Promise<AxiosResponse> => {
      const url = config.url || '';

      if (url.includes('/auth/refresh')) {
        refreshCallCount++;
        // Small delay to simulate async network roundtrip
        await new Promise((resolve) => setTimeout(resolve, 30));
        return {
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
          data: {
            accessToken: 'refreshed-access-token-999',
            expiresIn: 900,
          },
        };
      }

      requestAttempts[url] = (requestAttempts[url] || 0) + 1;

      // If request has old token or no token, return 401
      const authHeader = config.headers?.Authorization;
      if (authHeader !== 'Bearer refreshed-access-token-999') {
        const error = new AxiosError(
          'Request failed with status code 401',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 401,
            statusText: 'Unauthorized',
            headers: {},
            config,
            data: { title: 'Unauthorized', status: 401 },
          },
        );
        return Promise.reject(error);
      }

      // If request was retried with new token, succeed
      return {
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
        data: { success: true, url },
      };
    };

    // Fire 3 simultaneous API requests that will encounter 401
    const [resCart, resMe, resOrders] = await Promise.all([
      http.get('/cart'),
      http.get('/me'),
      http.get('/orders'),
    ]);

    // Verify only ONE refresh call occurred
    expect(refreshCallCount).toBe(1);

    // Verify all 3 requests eventually succeeded with new token
    expect(resCart.data).toEqual({ success: true, url: '/cart' });
    expect(resMe.data).toEqual({ success: true, url: '/me' });
    expect(resOrders.data).toEqual({ success: true, url: '/orders' });

    // Verify store has updated access token
    expect(useAuthStore.getState().token).toBe('refreshed-access-token-999');
  });

  it('rejects queued requests and clears auth state when refresh fails', async () => {
    let refreshCallCount = 0;

    http.defaults.adapter = async (config: InternalAxiosRequestConfig): Promise<AxiosResponse> => {
      const url = config.url || '';

      if (url.includes('/auth/refresh')) {
        refreshCallCount++;
        const error = new AxiosError(
          'Refresh token revoked',
          'ERR_BAD_REQUEST',
          config,
          null,
          {
            status: 401,
            statusText: 'Unauthorized',
            headers: {},
            config,
            data: { title: 'Refresh token expired', status: 401 },
          },
        );
        return Promise.reject(error);
      }

      if (url.includes('/auth/logout')) {
        return {
          status: 200,
          statusText: 'OK',
          headers: {},
          config,
          data: { message: 'Logged out' },
        };
      }

      // Initial request returns 401
      const error = new AxiosError(
        'Unauthorized',
        'ERR_BAD_REQUEST',
        config,
        null,
        {
          status: 401,
          statusText: 'Unauthorized',
          headers: {},
          config,
          data: { title: 'Unauthorized', status: 401 },
        },
      );
      return Promise.reject(error);
    };

    const logoutSpy = vi.spyOn(useAuthStore.getState(), 'logout');

    const [result1, result2] = await Promise.allSettled([
      http.get('/cart'),
      http.get('/me'),
    ]);

    expect(result1.status).toBe('rejected');
    expect(result2.status).toBe('rejected');
    expect(refreshCallCount).toBe(1);
    expect(logoutSpy).toHaveBeenCalled();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});
