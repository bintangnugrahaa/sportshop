import React from 'react';
import { MapPin, Phone, User, Check, Edit2, Trash2 } from 'lucide-react';
import { Address } from '@/types';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

interface AddressCardProps {
  address: Address;
  onEdit: (address: Address) => void;
  onDelete: (id: string) => void;
  onSetDefault: (id: string) => void;
  isActionLoading?: boolean;
  selectable?: boolean;
  selected?: boolean;
  onSelect?: (address: Address) => void;
}

export const AddressCard: React.FC<AddressCardProps> = ({
  address,
  onEdit,
  onDelete,
  onSetDefault,
  isActionLoading = false,
  selectable = false,
  selected = false,
  onSelect,
}) => {
  return (
    <div
      onClick={selectable && onSelect ? () => onSelect(address) : undefined}
      className={`relative rounded-lg border p-4 sm:p-5 transition-all ${
        selectable
          ? selected
            ? 'border-primary bg-primary/5 ring-2 ring-primary cursor-pointer'
            : 'border-border bg-card hover:border-primary/50 cursor-pointer'
          : 'border-border bg-card'
      }`}
      data-testid={`address-card-${address.id}`}
    >
      {/* Header: Label & Default Badge */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <span className="font-bold text-foreground text-sm uppercase tracking-wide">
            {address.label}
          </span>
          {address.isDefault && (
            <Badge variant="default" className="bg-primary text-white text-[10px] px-2 py-0.5">
              Utama
            </Badge>
          )}
        </div>

        {selectable && selected && (
          <div className="flex h-5 w-5 items-center justify-center rounded-full bg-primary text-white">
            <Check className="h-3.5 w-3.5 stroke-[3]" />
          </div>
        )}
      </div>

      {/* Recipient Details */}
      <div className="mt-3 space-y-1.5 text-xs sm:text-sm text-foreground/90">
        <div className="flex items-center gap-2 font-medium">
          <User className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
          <span>{address.recipientName}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <Phone className="h-3.5 w-3.5 flex-shrink-0" />
          <span>{address.phone}</span>
        </div>
        <div className="flex items-start gap-2 text-muted-foreground pt-1">
          <MapPin className="h-3.5 w-3.5 flex-shrink-0 mt-0.5 text-primary" />
          <div>
            <p className="text-foreground">{address.street}</p>
            <p className="text-xs text-muted-foreground mt-0.5">
              {[address.district, address.city, address.provinceName].filter(Boolean).join(', ')}{' '}
              {address.postalCode}
            </p>
          </div>
        </div>
      </div>

      {/* Actions (if not pure selector mode) */}
      {!selectable && (
        <div className="mt-4 flex flex-wrap items-center gap-2 border-t pt-3">
          {!address.isDefault && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onSetDefault(address.id)}
              disabled={isActionLoading}
              className="text-xs h-7 px-2.5"
            >
              Jadikan Utama
            </Button>
          )}

          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => onEdit(address)}
            disabled={isActionLoading}
            className="text-xs h-7 px-2.5 text-foreground hover:text-primary"
          >
            <Edit2 className="mr-1 h-3 w-3" />
            Ubah
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => onDelete(address.id)}
            disabled={isActionLoading || address.isDefault}
            className="text-xs h-7 px-2.5 text-destructive hover:bg-destructive/10"
            title={address.isDefault ? 'Alamat utama tidak dapat dihapus' : undefined}
          >
            <Trash2 className="mr-1 h-3 w-3" />
            Hapus
          </Button>
        </div>
      )}
    </div>
  );
};
