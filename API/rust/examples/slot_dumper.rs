use dergwasm::Slot;

fn main() {
    println!("Printing the slot tree:");
    let root = Slot::root_slot();
    print_layer(root, 0);
}

fn print_layer(slot: Slot, indentation: usize) {
    println!("{}{:?}: {}", " ".repeat(indentation), slot, slot.name().as_deref().unwrap_or("<null>"));
    for child in slot.children().iter() {
        print_layer(child, indentation + 1);
    }
}
