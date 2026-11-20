import React from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  MapPin,
  Truck,
  Plus,
  ShieldCheck,
  AlertTriangle,
  Loader2,
  Lock,
} from 'lucide-react';
import { useCart } from '@/features/cart/api';
import {
  useAddresses,
  useShippingProvinces,
  useCreateAddress,
} from '@/features/account/api/addressesApi';
import { AddressCard } from '@/features/account/components/AddressCard';
import { AddressFormDialog } from '@/features/account/components/AddressFormDialog';
import { useShippingQuote, useCheckout } from '../api/checkoutApi';
import { Address } from '@/types';
import { AddressFormData } from '@/features/account/schemas/addressSchema';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { loadSnapScript } from '@/lib/snap';
import { toast } from '@/lib/toast';

export const CheckoutPage: React.FC = () => {
  const navigate = useNavigate();
  const { data: cart, isLoading: isCartLoading } = useCart();
  const { data: addresses = [], isLoading: isAddressesLoading } = useAddresses();
  const { data: provinces = [] } = useShippingProvinces();

  const [selectedAddressId, setSelectedAddressId] = React.useState<string | null>(null);
  const [isAddressModalOpen, setIsAddressModalOpen] = React.useState(false);
  const [idempotencyKey, setIdempotencyKey] = React.useState<string>(() => crypto.randomUUID());
  const [isProcessingCheckout, setIsProcessingCheckout] = React.useState(false);

  const createAddressMutation = useCreateAddress();
  const checkoutMutation = useCheckout();

  // Load Midtrans snap.js in background
  React.useEffect(() => {
    void loadSnapScript().catch(() => {});
  }, []);

  // Set default address when addresses load
  React.useEffect(() => {
    if (addresses.length > 0 && !selectedAddressId) {
      const defaultAddr = addresses.find((a) => a.isDefault) ?? addresses[0];
      if (defaultAddr) {
        setSelectedAddressId(defaultAddr.id);
      }
    }
  }, [addresses, selectedAddressId]);

  const {
    data: quote,
    isLoading: isQuoteLoading,
    error: quoteError,
  } = useShippingQuote(selectedAddressId);

  const handleCreateAddress = async (formData: AddressFormData) => {
    try {
      const newAddress = await createAddressMutation.mutateAsync(formData);
      setSelectedAddressId(newAddress.id);
      setIsAddressModalOpen(false);
      toast.success('Alamat berhasil ditambahkan.');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menambahkan alamat.';
      toast.error(msg);
    }
  };

  const handleCheckout = async () => {
    if (!selectedAddressId) {
      toast.error('Silakan pilih alamat pengiriman.');
      return;
    }

    if (!quote) {
      toast.error('Tarif ongkos kirim belum berhasil dihitung. Silakan coba lagi.');
      return;
    }

    // Prevent double clicking
    if (isProcessingCheckout || checkoutMutation.isPending) {
      return;
    }

    setIsProcessingCheckout(true);

    try {
      const order = await checkoutMutation.mutateAsync({
        addressId: selectedAddressId,
        idempotencyKey,
      });

      // Regenerate idempotency key for future attempts
      setIdempotencyKey(crypto.randomUUID());

      toast.success(`Pesanan ${order.orderNumber} berhasil dibuat!`);

      // If Snap token is available, open Midtrans Snap popup
      if (order.snapToken && window.snap) {
        window.snap.pay(order.snapToken, {
          onSuccess: () => {
            navigate(`/checkout/status/${order.orderNumber}`);
          },
          onPending: () => {
            navigate(`/checkout/status/${order.orderNumber}`);
          },
          onError: () => {
            navigate(`/checkout/status/${order.orderNumber}`);
          },
          onClose: () => {
            navigate(`/checkout/status/${order.orderNumber}`);
          },
        });
      } else {
        // Token unavailable or snap failed to load, proceed to status page for manual/retry pay
        navigate(`/checkout/status/${order.orderNumber}`);
      }
    } catch (err: unknown) {
      setIsProcessingCheckout(false);
      const msg = err instanceof Error ? err.message : 'Gagal membuat pesanan.';
      toast.error(msg);
    }
  };

  if (isCartLoading || isAddressesLoading) {
    return (
      <div className="container mx-auto px-4 py-16 flex justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container mx-auto px-4 py-16 text-center max-w-md">
        <h1 className="text-2xl font-bold">Keranjang Belanja Kosong</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Tidak ada produk yang siap di-checkout. Silakan pilih produk terlebih dahulu.
        </p>
        <Button className="mt-6" onClick={() => navigate('/produk')}>
          Jelajahi Produk
        </Button>
      </div>
    );
  }

  const selectedAddress = addresses.find((a) => a.id === selectedAddressId);

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8">
      {/* Title */}
      <div className="mb-6">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground flex items-center gap-2">
          <Lock className="h-6 w-6 text-primary" />
          <span>Checkout Pembayaran</span>
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Selesaikan pesanan Anda dengan memilih alamat dan konfirmasi ringkasan biaya.
        </p>
      </div>

      {/* Cart has issues warning */}
      {cart.hasIssues && (
        <div className="mb-6 flex items-start gap-3 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
          <AlertTriangle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div>
            <h4 className="font-semibold">Keranjang Memerlukan Penyesuaian</h4>
            <p className="mt-1 text-xs">
              Beberapa produk di keranjang Anda mengalami perubahan harga atau kekurangan stok.
              Harap kembali ke keranjang untuk menyesuaikan sebelum checkout.
            </p>
            <Link
              to="/keranjang"
              className="mt-2 inline-block font-semibold underline hover:text-foreground"
            >
              Kembali ke Keranjang
            </Link>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        {/* Left Column: Address Selection & Items Preview */}
        <div className="lg:col-span-8 space-y-6">
          {/* Section 1: Alamat Pengiriman */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between border-b pb-4">
              <CardTitle className="text-lg flex items-center gap-2">
                <MapPin className="h-5 w-5 text-primary" />
                <span>Alamat Pengiriman</span>
              </CardTitle>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsAddressModalOpen(true)}
                className="text-xs"
              >
                <Plus className="mr-1 h-3.5 w-3.5" />
                Tambah Alamat
              </Button>
            </CardHeader>
            <CardContent className="pt-5">
              {addresses.length === 0 ? (
                <div className="rounded-lg border border-dashed p-6 text-center">
                  <p className="text-sm text-muted-foreground mb-3">
                    Anda belum memiliki alamat pengiriman tersimpan.
                  </p>
                  <Button size="sm" onClick={() => setIsAddressModalOpen(true)}>
                    <Plus className="mr-1 h-4 w-4" />
                    Tambah Alamat Sekarang
                  </Button>
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {addresses.map((address) => (
                    <AddressCard
                      key={address.id}
                      address={address}
                      selectable={true}
                      selected={address.id === selectedAddressId}
                      onSelect={(addr: Address) => setSelectedAddressId(addr.id)}
                      onEdit={() => {}}
                      onDelete={() => {}}
                      onSetDefault={() => {}}
                    />
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Section 2: Items Preview */}
          <Card>
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-lg flex items-center justify-between">
                <span>Item Pesanan ({cart.totalItems})</span>
                <Link to="/keranjang" className="text-xs font-normal text-primary hover:underline">
                  Ubah Keranjang
                </Link>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4 divide-y">
              {cart.items.map((item) => (
                <div key={item.id} className="py-3 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    {item.primaryImageUrl ? (
                      <img
                        src={item.primaryImageUrl}
                        alt={item.productName}
                        className="h-12 w-12 rounded object-cover border bg-muted"
                      />
                    ) : (
                      <div className="h-12 w-12 rounded border bg-muted flex items-center justify-center text-[10px] text-muted-foreground">
                        Item
                      </div>
                    )}
                    <div>
                      <p className="text-sm font-medium line-clamp-1">{item.productName}</p>
                      <p className="text-xs text-muted-foreground">
                        {[item.optionSize, item.optionColor].filter(Boolean).join(' / ') ||
                          item.sku}{' '}
                        × {item.quantity}
                      </p>
                    </div>
                  </div>
                  <Price price={item.lineTotal} className="font-semibold text-sm" />
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Ringkasan & Tombol Buat Pesanan */}
        <div className="lg:col-span-4">
          <Card className="sticky top-24">
            <CardHeader className="border-b pb-4">
              <CardTitle className="text-lg">Ringkasan Pembayaran</CardTitle>
            </CardHeader>
            <CardContent className="pt-4 space-y-4">
              {/* Shipping info */}
              {selectedAddress && (
                <div className="rounded-md border bg-muted/30 p-3 text-xs space-y-1">
                  <div className="flex items-center gap-1.5 font-medium text-foreground">
                    <Truck className="h-4 w-4 text-primary" />
                    <span>Tujuan: {selectedAddress.city}, {selectedAddress.provinceName}</span>
                  </div>
                  {quote && (
                    <p className="text-muted-foreground">
                      Estimasi: {quote.estimatedDaysMin}–{quote.estimatedDaysMax} hari kerja
                      (Berat tagih: {quote.billableWeightKg} kg)
                    </p>
                  )}
                </div>
              )}

              {/* Price Breakdown */}
              <div className="space-y-2 text-sm">
                <div className="flex justify-between text-muted-foreground">
                  <span>Subtotal Produk</span>
                  <Price price={cart.subtotal} />
                </div>

                <div className="flex justify-between text-muted-foreground items-center">
                  <span>Ongkos Kirim</span>
                  {isQuoteLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                  ) : quote ? (
                    <Price price={quote.shippingFee} />
                  ) : (
                    <span className="italic text-xs text-muted-foreground">Pilih alamat</span>
                  )}
                </div>

                {quoteError && (
                  <p className="text-xs text-destructive">
                    Gagal mengambil tarif ongkir untuk alamat ini.
                  </p>
                )}

                <div className="border-t pt-3 flex justify-between font-bold text-base">
                  <span>Total Pembayaran</span>
                  <Price
                    price={quote ? quote.estimatedTotal : cart.subtotal}
                    className="text-xl text-primary font-black"
                  />
                </div>
              </div>

              {/* Checkout Button: Double click prevention */}
              <Button
                className="w-full h-12 text-base font-semibold"
                disabled={
                  !selectedAddressId ||
                  isQuoteLoading ||
                  !quote ||
                  cart.hasIssues ||
                  isProcessingCheckout ||
                  checkoutMutation.isPending
                }
                onClick={handleCheckout}
                data-testid="checkout-submit-btn"
              >
                {isProcessingCheckout || checkoutMutation.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Memproses Pesanan...
                  </>
                ) : (
                  'Buat Pesanan & Bayar'
                )}
              </Button>

              <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground pt-1">
                <ShieldCheck className="h-4 w-4 text-emerald-600" />
                <span>Didukung oleh Midtrans Snap Payment Gateway</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Address Form Dialog */}
      <AddressFormDialog
        isOpen={isAddressModalOpen}
        onClose={() => setIsAddressModalOpen(false)}
        onSubmit={handleCreateAddress}
        provinces={provinces}
        isSubmitting={createAddressMutation.isPending}
      />
    </div>
  );
};

export default CheckoutPage;
