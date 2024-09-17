fn main() {
    add_export("malloc");
    add_export("free");
}

fn add_export(export: &str) {
    linker_flag("--export");
    linker_flag(export);
}

fn linker_flag(flag: &str) {
    println!("cargo:rustc-link-arg={}", flag);
}
