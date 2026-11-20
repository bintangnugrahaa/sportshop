export interface User {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  role: 'Customer' | 'Admin';
  emailConfirmed?: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresIn: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface Category {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  sortOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
  subCategories?: Category[];
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string | null;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  price: number;
  weightGram: number;
  stockQuantity: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface ProductImage {
  id: string;
  productId: string;
  url: string;
  altText?: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface Product {
  id: string;
  categoryId: string;
  categoryName?: string;
  categorySlug?: string;
  brandId: string;
  brandName?: string;
  brandSlug?: string;
  name: string;
  slug: string;
  description: string;
  basePrice: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
  variants?: ProductVariant[];
  images?: ProductImage[];
  primaryImageUrl?: string;
  inStock: boolean;
  minPrice?: number;
  maxPrice?: number;
  totalStock?: number;
}

export interface CartItem {
  id: string;
  variantId: string;
  productId: string;
  productName: string;
  productSlug: string;
  sku: string;
  optionSize?: string | null;
  optionColor?: string | null;
  primaryImageUrl?: string | null;
  unitPrice: number;
  priceAtAdd: number;
  quantity: number;
  lineTotal: number;
  weightGram: number;
  totalWeightGram: number;
  stockQuantity: number;
  isActive: boolean;
  isPriceChanged: boolean;
  isStockInsufficient: boolean;
  isAvailable: boolean;
  statusNote?: string | null;
}

export interface Cart {
  id: string;
  userId: string;
  items: CartItem[];
  totalItems: number;
  totalWeightGram: number;
  subtotal: number;
  hasIssues: boolean;
}

export type OrderStatus =
  | 'PendingPayment'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Completed'
  | 'Cancelled'
  | 'Expired';

export interface Address {
  id: string;
  userId: string;
  label: string;
  recipientName: string;
  phone: string;
  provinceCode: string;
  provinceName: string;
  city: string;
  district?: string | null;
  postalCode: string;
  street: string;
  isDefault: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateAddressInput {
  label: string;
  recipientName: string;
  phone: string;
  provinceCode: string;
  city: string;
  district?: string | null;
  postalCode: string;
  street: string;
  isDefault: boolean;
}

export type UpdateAddressInput = CreateAddressInput;

export interface ShippingRate {
  provinceCode: string;
  provinceName: string;
  baseFee: number;
  perKgFee: number;
  estimatedDaysMin: number;
  estimatedDaysMax: number;
  isActive: boolean;
  updatedAt?: string | null;
}

export interface ShippingQuote {
  addressId: string;
  recipientName: string;
  provinceCode: string;
  provinceName: string;
  city: string;
  postalCode: string;
  totalWeightGram: number;
  billableWeightKg: number;
  baseFee: number;
  perKgFee: number;
  shippingFee: number;
  estimatedDaysMin: number;
  estimatedDaysMax: number;
  cartSubtotal: number;
  estimatedTotal: number;
}

export interface OrderShippingAddress {
  recipientName: string;
  phone: string;
  provinceCode: string;
  provinceName: string;
  city: string;
  postalCode: string;
  street: string;
}

export interface OrderItem {
  id: string;
  variantId: string;
  productName: string;
  variantLabel: string;
  sku: string;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderStatusHistory {
  id: string;
  fromStatus?: string | null;
  toStatus: string;
  note?: string | null;
  createdAt: string;
}

export interface Payment {
  id: string;
  status: string;
  grossAmount: number;
  snapToken?: string | null;
  snapRedirectUrl?: string | null;
  paymentType?: string | null;
  settledAt?: string | null;
  createdAt: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  userId: string;
  status: OrderStatus;
  subtotal: number;
  shippingFee: number;
  total: number;
  shippingAddress: OrderShippingAddress;
  totalWeightGram: number;
  courier?: string | null;
  trackingNumber?: string | null;
  expiresAt: string;
  paidAt?: string | null;
  shippedAt?: string | null;
  completedAt?: string | null;
  cancelledAt?: string | null;
  cancelReason?: string | null;
  createdAt: string;
  items: OrderItem[];
  statusHistories: OrderStatusHistory[];
  payment?: Payment | null;
  snapToken?: string | null;
  snapRedirectUrl?: string | null;
  paymentStatus?: string | null;
}

export interface PayOrderResponse {
  orderNumber: string;
  snapToken?: string | null;
  snapRedirectUrl?: string | null;
  paymentStatus: string;
}
